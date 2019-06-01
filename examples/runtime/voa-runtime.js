(function(global) {
    'use strict';

    // #region 响应式内核

    let activeObserver = null;
    const observerStack = [];
    let batchDepth = 0;
    let pendingEffects = new Set();
    let schedulerQueued = false;

    function pushObserver(observer) {
        observerStack.push(observer);
        activeObserver = observer;
    }

    function popObserver() {
        observerStack.pop();
        activeObserver = observerStack[observerStack.length - 1] || null;
    }

    function runInObserver(fn, observer) {
        pushObserver(observer);
        try {
            return fn();
        } finally {
            popObserver();
        }
    }

    function untrack(fn) {
        const prev = activeObserver;
        activeObserver = null;
        try {
            return fn();
        } finally {
            activeObserver = prev;
        }
    }

    function batch(fn) {
        batchDepth++;
        try {
            fn();
        } finally {
            batchDepth--;
            if (batchDepth === 0) {
                flushEffects();
            }
        }
    }

    function flushEffects() {
        if (schedulerQueued) return;
        schedulerQueued = true;
        queueMicrotask(() => {
            schedulerQueued = false;
            const effects = pendingEffects;
            pendingEffects = new Set();
            let iterations = 0;
            while (effects.size > 0 && iterations < 100) {
                const batch = effects;
                pendingEffects = new Set();
                batch.forEach(effect => {
                    if (effect._alive) {
                        effect._run();
                    }
                });
                effects = pendingEffects;
                iterations++;
            }
            if (iterations >= 100) {
                console.warn('Effect 循环超过 100 次迭代，可能存在无限循环');
            }
        });
    }

    function scheduleEffect(effect) {
        pendingEffects.add(effect);
        if (batchDepth === 0) {
            flushEffects();
        }
    }

    // #endregion

    // #region 生命周期

    const cleanupStack = [];
    const mountQueue = [];

    function onMount(fn) {
        mountQueue.push(fn);
    }

    function onCleanup(fn) {
        if (cleanupStack.length > 0) {
            cleanupStack[cleanupStack.length - 1].push(fn);
        }
    }

    function runCleanupScope(fn) {
        const cleanups = [];
        cleanupStack.push(cleanups);
        try {
            const result = fn();
            if (mountQueue.length > 0) {
                const mounts = mountQueue.splice(0);
                queueMicrotask(() => {
                    mounts.forEach(m => {
                        try { m(); } catch (e) { console.error('onMount 错误:', e); }
                    });
                });
            }
            return result;
        } finally {
            cleanupStack.pop();
            if (cleanups.length > 0) {
                const toRun = cleanups.splice(0);
                onCleanup(() => toRun.forEach(c => {
                    try { c(); } catch (e) { console.error('onCleanup 错误:', e); }
                }));
            }
        }
    }

    // #endregion

    // #region Signal

    function createSignal(initialValue) {
        let value = typeof initialValue === 'function' ? initialValue() : initialValue;
        const subscribers = new Set();

        const getter = () => {
            if (activeObserver) {
                subscribers.add(activeObserver);
                activeObserver._sources.add(subscribers);
            }
            return value;
        };

        getter._isSignal = true;

        const setter = (newValue) => {
            const nextValue = typeof newValue === 'function' ? newValue(value) : newValue;
            if (!Object.is(nextValue, value)) {
                value = nextValue;
                notifySubscribers(subscribers);
            }
        };

        return [getter, setter];
    }

    function notifySubscribers(subscribers) {
        const toNotify = new Set(subscribers);
        toNotify.forEach(observer => {
            if (observer._kind === 'effect') {
                scheduleEffect(observer);
            } else if (observer._kind === 'memo') {
                observer._markDirty();
            } else if (observer._kind === 'render') {
                scheduleEffect(observer);
            }
        });
    }

    // #endregion

    // #region Effect

    function createEffect(fn) {
        const effect = {
            _kind: 'effect',
            _fn: fn,
            _alive: true,
            _sources: new Set(),
            _run() {
                if (!this._alive) return;
                this._cleanup();
                this._sources.forEach(source => source.delete(this));
                this._sources.clear();
                const result = runInObserver(this._fn, this);
                if (typeof result === 'function') {
                    this._cleanupFn = result;
                }
            },
            _cleanupFn: null,
            _cleanup() {
                if (this._cleanupFn) {
                    this._cleanupFn();
                    this._cleanupFn = null;
                }
            },
            dispose() {
                this._alive = false;
                this._cleanup();
                this._sources.forEach(source => source.delete(this));
                this._sources.clear();
            }
        };

        effect._run();
        return effect;
    }

    // #endregion

    // #region Memo

    function createMemo(fn) {
        let value;
        let dirty = true;
        const memoSubscribers = new Set();

        const memo = {
            _kind: 'memo',
            _fn: fn,
            _alive: true,
            _sources: new Set(),
            _dirty: true,
            _markDirty() {
                if (!this._dirty) {
                    this._dirty = true;
                    notifySubscribers(memoSubscribers);
                }
            },
            _run() {
                if (!this._alive) return;
                this._sources.forEach(source => source.delete(this));
                this._sources.clear();
                value = runInObserver(this._fn, this);
                this._dirty = false;
            },
            dispose() {
                this._alive = false;
                this._sources.forEach(source => source.delete(this));
                this._sources.clear();
            }
        };

        const getter = () => {
            if (memo._dirty) {
                memo._run();
            }
            if (activeObserver) {
                memoSubscribers.add(activeObserver);
                activeObserver._sources.add(memoSubscribers);
            }
            return value;
        };

        getter._isSignal = true;
        return getter;
    }

    // #endregion

    // #region DOM 操作

    function createElement(tag) {
        return document.createElement(tag);
    }

    function createTextNode(text) {
        return document.createTextNode(text);
    }

    function setAttribute(el, name, value) {
        if (value === null || value === undefined || value === false) {
            el.removeAttribute(name);
        } else if (value === true) {
            el.setAttribute(name, '');
        } else {
            el.setAttribute(name, String(value));
        }
    }

    function setProperty(el, name, value) {
        try {
            el[name] = value;
        } catch (e) {
            console.warn('设置属性 ' + name + ' 失败:', e);
        }
    }

    function insertNode(parent, node, before) {
        if (before) {
            parent.insertBefore(node, before);
        } else {
            parent.appendChild(node);
        }
    }

    function removeNode(node) {
        if (node && node.parentNode) {
            node.parentNode.removeChild(node);
        }
    }

    function dynamicText(fn) {
        const textNode = createTextNode('');
        createEffect(() => {
            const value = fn();
            textNode.textContent = value == null ? '' : String(value);
        });
        return textNode;
    }

    function dynamicAttribute(el, name, fn) {
        if (name.startsWith('on')) {
            const eventType = name.slice(2).toLowerCase();
            let lastHandler = null;
            createEffect(() => {
                const handler = fn();
                if (lastHandler) {
                    el.removeEventListener(eventType, lastHandler);
                }
                lastHandler = handler;
                if (handler) {
                    el.addEventListener(eventType, handler);
                }
            });
        } else if (name === 'style' && typeof fn() === 'object') {
            createEffect(() => {
                const styleObj = fn();
                if (styleObj) {
                    Object.keys(styleObj).forEach(key => {
                        el.style[key] = styleObj[key];
                    });
                }
            });
        } else if (name === 'class' || name === 'className') {
            createEffect(() => {
                const value = fn();
                if (typeof value === 'object' && value !== null) {
                    el.className = Object.keys(value).filter(k => value[k]).join(' ');
                } else {
                    el.className = value == null ? '' : String(value);
                }
            });
        } else {
            createEffect(() => {
                const value = fn();
                if (name === 'value') {
                    el.value = value == null ? '' : String(value);
                } else if (name === 'checked') {
                    el.checked = !!value;
                } else {
                    setAttribute(el, name, value);
                }
            });
        }
    }

    function conditional(parent, conditionFn, trueFactory, falseFactory) {
        const anchor = createTextNode('');
        let currentNodes = [];
        let currentIsTrue = undefined;
        let currentCleanups = [];

        insertNode(parent, anchor);

        createEffect(() => {
            const isTrue = !!conditionFn();
            if (isTrue === currentIsTrue) return;
            currentIsTrue = isTrue;

            currentCleanups.forEach(c => { try { c(); } catch(e) {} });
            currentCleanups = [];

            currentNodes.forEach(node => removeNode(node));
            currentNodes = [];

            const factory = isTrue ? trueFactory : falseFactory;
            if (!factory) return;

            const fragment = document.createDocumentFragment();
            const result = runCleanupScope(factory);

            if (Array.isArray(result)) {
                result.forEach(node => {
                    if (node instanceof Node) {
                        fragment.appendChild(node);
                        currentNodes.push(node);
                    }
                });
            } else if (result instanceof Node) {
                fragment.appendChild(result);
                currentNodes.push(result);
            }

            if (anchor.nextSibling) {
                parent.insertBefore(fragment, anchor.nextSibling);
            } else {
                parent.appendChild(fragment);
            }
        });

        return anchor;
    }

    function listMap(itemsFn, mapFn, parent) {
        const anchor = createTextNode('');
        if (parent) insertNode(parent, anchor);

        let currentEntries = [];

        createEffect(() => {
            const items = itemsFn();
            const oldEntries = currentEntries;
            currentEntries = [];

            const fragment = document.createDocumentFragment();

            items.forEach((item, index) => {
                let entry;
                if (index < oldEntries.length && oldEntries[index]._item === item) {
                    entry = oldEntries[index];
                } else {
                    if (index < oldEntries.length) {
                        const old = oldEntries[index];
                        old.nodes.forEach(node => removeNode(node));
                        if (old.effect) old.effect.dispose();
                    }

                    const entryNodes = [];
                    entry = { nodes: entryNodes, effect: null, _item: item };

                    entry.effect = createEffect(() => {
                        entryNodes.forEach(node => removeNode(node));
                        entryNodes.length = 0;

                        const result = mapFn(item, index);

                        if (Array.isArray(result)) {
                            result.forEach(node => {
                                if (node instanceof Node) {
                                    fragment.appendChild(node);
                                    entryNodes.push(node);
                                }
                            });
                        } else if (result instanceof Node) {
                            fragment.appendChild(result);
                            entryNodes.push(result);
                        }
                    });
                }

                entry.nodes.forEach(node => {
                    if (node.parentNode !== fragment && node.parentNode !== (parent || anchor.parentNode)) {
                        fragment.appendChild(node);
                    }
                });

                currentEntries.push(entry);
            });

            for (let i = items.length; i < oldEntries.length; i++) {
                const old = oldEntries[i];
                old.nodes.forEach(node => removeNode(node));
                if (old.effect) old.effect.dispose();
            }

            if (anchor.nextSibling) {
                anchor.parentNode.insertBefore(fragment, anchor.nextSibling);
            } else {
                anchor.parentNode.appendChild(fragment);
            }
        });

        return anchor;
    }

    // #endregion

    // #region 组件系统

    function createComponent(factory, props) {
        return runCleanupScope(() => factory(props));
    }

    // #endregion

    // #region 挂载

    function mount(selector, factory) {
        const root = typeof selector === 'string'
            ? document.querySelector(selector)
            : selector;

        if (!root) {
            throw new Error('挂载目标未找到: ' + selector);
        }

        root.innerHTML = '';

        const result = runCleanupScope(factory);

        if (Array.isArray(result)) {
            result.forEach(node => {
                if (node instanceof Node) {
                    root.appendChild(node);
                }
            });
        } else if (result instanceof Node) {
            root.appendChild(result);
        }
    }

    // #endregion

    // #region 模板辅助

    function h(tag, props) {
        const children = Array.prototype.slice.call(arguments, 2);
        const el = createElement(tag);

        if (props) {
            Object.keys(props).forEach(key => {
                const value = props[key];
                if (key === 'class' || key === 'className') {
                    if (typeof value === 'function') {
                        dynamicAttribute(el, 'class', value);
                    } else if (typeof value === 'object' && value !== null) {
                        el.className = Object.keys(value).filter(k => value[k]).join(' ');
                    } else {
                        el.className = String(value);
                    }
                } else if (key === 'style') {
                    if (typeof value === 'function') {
                        dynamicAttribute(el, 'style', value);
                    } else if (typeof value === 'object' && value !== null) {
                        Object.keys(value).forEach(k => { el.style[k] = value[k]; });
                    } else {
                        el.setAttribute('style', String(value));
                    }
                } else if (key.startsWith('on')) {
                    const eventType = key.slice(2).toLowerCase();
                    if (typeof value === 'function') {
                        el.addEventListener(eventType, value);
                    }
                } else if (typeof value === 'function') {
                    dynamicAttribute(el, key, value);
                } else {
                    setAttribute(el, key, value);
                }
            });
        }

        children.forEach(child => {
            if (child == null || child === false) return;
            if (typeof child === 'function') {
                el.appendChild(dynamicText(child));
            } else if (child instanceof Node) {
                el.appendChild(child);
            } else if (Array.isArray(child)) {
                child.forEach(c => {
                    if (c instanceof Node) {
                        el.appendChild(c);
                    } else if (c != null && c !== false) {
                        el.appendChild(createTextNode(String(c)));
                    }
                });
            } else {
                el.appendChild(createTextNode(String(child)));
            }
        });

        return el;
    }

    function Fragment() {
        const children = Array.prototype.slice.call(arguments);
        const fragment = document.createDocumentFragment();
        children.forEach(child => {
            if (child instanceof Node) {
                fragment.appendChild(child);
            }
        });
        return fragment;
    }

    // #endregion

    // #region Island Architecture

    const IslandStrategy = {
        Idle: 'idle',
        Visible: 'visible',
        Interaction: 'interaction',
        Media: 'media',
        Load: 'load'
    };

    const islandRegistry = new Map();
    const islandInstances = new Map();
    let hydrationScheduler = null;

    function registerIsland(name, config) {
        islandRegistry.set(name, {
            name,
            factory: config.factory,
            strategy: config.strategy || IslandStrategy.Load,
            mediaQuery: config.mediaQuery || null,
            props: config.props || {},
            bundle: config.bundle || null
        });
    }

    function getIslandConfig(name) {
        return islandRegistry.get(name);
    }

    function getAllIslands() {
        return Array.from(islandRegistry.values());
    }

    class HydrationScheduler {
        constructor() {
            this.pending = new Map();
            this.observer = null;
            this.idleCallback = null;
            this.init();
        }

        init() {
            if (typeof IntersectionObserver !== 'undefined') {
                this.observer = new IntersectionObserver(this.onVisible.bind(this), {
                    rootMargin: '100px'
                });
            }

            if (typeof requestIdleCallback !== 'undefined') {
                this.idleCallback = requestIdleCallback;
            } else {
                this.idleCallback = (cb) => setTimeout(() => cb({ timeRemaining: 50 }), 1);
            }
        }

        schedule(config, element) {
            const id = element.dataset.islandId || `island-${Date.now()}-${Math.random().toString(36).slice(2)}`;
            element.dataset.islandId = id;

            const islandConfig = {
                id,
                config,
                element,
                hydrated: false
            };

            this.pending.set(id, islandConfig);

            switch (config.strategy) {
                case IslandStrategy.Idle:
                    this.idleCallback(() => this.hydrate(islandConfig));
                    break;
                case IslandStrategy.Visible:
                    if (this.observer) {
                        this.observer.observe(element);
                    } else {
                        this.hydrate(islandConfig);
                    }
                    break;
                case IslandStrategy.Interaction:
                    this.attachInteractionListener(islandConfig);
                    break;
                case IslandStrategy.Media:
                    this.watchMediaQuery(islandConfig);
                    break;
                case IslandStrategy.Load:
                default:
                    this.hydrate(islandConfig);
                    break;
            }
        }

        onVisible(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const island = this.pending.get(entry.target.dataset.islandId);
                    if (island && !island.hydrated) {
                        this.observer.unobserve(entry.target);
                        this.hydrate(island);
                    }
                }
            });
        }

        attachInteractionListener(island) {
            const events = ['click', 'touchstart', 'mouseover', 'input', 'focus'];
            const handler = () => {
                events.forEach(e => island.element.removeEventListener(e, handler));
                this.hydrate(island);
            };
            events.forEach(e => island.element.addEventListener(e, handler, { once: true, passive: true }));
        }

        watchMediaQuery(island) {
            if (!island.config.mediaQuery) {
                this.hydrate(island);
                return;
            }

            const mq = matchMedia(island.config.mediaQuery);
            if (mq.matches) {
                this.hydrate(island);
            } else {
                const listener = (e) => {
                    if (e.matches) {
                        mq.removeEventListener('change', listener);
                        this.hydrate(island);
                    }
                };
                mq.addEventListener('change', listener);
            }
        }

        hydrate(island) {
            if (island.hydrated) return;
            island.hydrated = true;
            this.pending.delete(island.id);

            if (island.config.factory) {
                const rendered = island.config.factory(island.config.props);
                if (rendered) {
                    if (island.config.shadowDOM) {
                        const shadow = island.element.attachShadow({ mode: 'open' });
                        shadow.appendChild(rendered);
                        if (island.config.styles) {
                            const styleEl = document.createElement('style');
                            styleEl.textContent = island.config.styles;
                            shadow.prepend(styleEl);
                        }
                    } else {
                        island.element.innerHTML = '';
                        island.element.appendChild(rendered);
                    }
                }
            } else if (island.config.bundle) {
                this.loadAndHydrate(island);
            }
        }

        async loadAndHydrate(island) {
            try {
                const module = await import(island.config.bundle);
                if (module.hydrate) {
                    module.hydrate(island.element, island.config.props);
                }
            } catch (e) {
                console.error(`Islands 加载失败 ${island.config.bundle}:`, e);
            }
        }
    }

    function getScheduler() {
        if (!hydrationScheduler) {
            hydrationScheduler = new HydrationScheduler();
        }
        return hydrationScheduler;
    }

    function hydrateIslands() {
        const scheduler = getScheduler();
        const elements = document.querySelectorAll('[data-island]');

        elements.forEach(el => {
            const name = el.dataset.island;
            const strategy = el.dataset.hydrate || IslandStrategy.Load;
            const mediaQuery = el.dataset.media || null;

            const config = islandRegistry.get(name);
            if (config) {
                scheduler.schedule({
                    ...config,
                    strategy,
                    mediaQuery
                }, el);
            }
        });
    }

    function mountIsland(name, element, props) {
        const config = islandRegistry.get(name);
        if (!config) {
            console.error(`Islands 未注册: ${name}`);
            return null;
        }

        const island = {
            id: element.dataset.islandId || `island-${Date.now()}`,
            config,
            element,
            hydrated: true
        };

        islandInstances.set(island.id, island);

        if (config.factory) {
            const rendered = config.factory({ ...config.props, ...props });
            if (rendered) {
                if (config.shadowDOM) {
                    const shadow = element.attachShadow({ mode: 'open' });
                    shadow.appendChild(rendered);
                    if (config.styles) {
                        const styleEl = document.createElement('style');
                        styleEl.textContent = config.styles;
                        shadow.prepend(styleEl);
                    }
                } else {
                    element.innerHTML = '';
                    element.appendChild(rendered);
                }
            }
        } else if (config.bundle) {
            IslandLoader.loadAndHydrate(name, element, props);
        }

        return island;
    }

    function unmountIsland(id) {
        const island = islandInstances.get(id);
        if (island && island.element.$__voaCleanup) {
            island.element.$__voaCleanup();
        }
        islandInstances.delete(id);
    }

    function createIslandElement(name, props, strategy) {
        const el = document.createElement('div');
        el.dataset.island = name;
        if (strategy) {
            el.dataset.hydrate = strategy;
        }
        return el;
    }

    // #endregion

    // #region Vue Bridge

    const VueBridge = {
        bridges: new Map(),

        async mount(componentUrl, props, host) {
            const shadow = host.attachShadow({ mode: 'open' });
            const container = document.createElement('div');
            shadow.appendChild(container);

            try {
                const vue = await import('https://unpkg.com/vue@3/dist/vue.esm-browser.js');
                const module = await import(componentUrl);
                const component = module.default || module;

                const app = vue.createApp({
                    render: () => vue.h(component, {
                        ...props,
                        onVs: (event, payload) => this.emit(host, event, payload)
                    })
                });

                app.mount(container);

                const bridge = {
                    app,
                    shadow,
                    container,
                    unmount: () => app.unmount()
                };

                this.bridges.set(host, bridge);
                return bridge;
            } catch (e) {
                console.error('Vue Bridge 挂载失败:', e);
                return null;
            }
        },

        unmount(host) {
            const bridge = this.bridges.get(host);
            if (bridge) {
                bridge.unmount();
                this.bridges.delete(host);
            }
        },

        emit(host, event, payload) {
            host.dispatchEvent(new CustomEvent(`voa:${event}`, { detail: payload }));
        }
    };

    // #endregion

    // #region React Bridge

    const ReactBridge = {
        bridges: new Map(),

        async mount(componentUrl, props, host) {
            const shadow = host.attachShadow({ mode: 'open' });
            const container = document.createElement('div');
            shadow.appendChild(container);

            try {
                const [reactModule, reactDomModule, reactJsxDevModule] = await Promise.all([
                    import('https://esm.sh/react@18'),
                    import('https://esm.sh/react-dom@18/client'),
                    import('https://esm.sh/react-jsx-dev-runtime@18')
                ]);

                const React = reactModule.default;
                const { createRoot } = reactDomModule;
                const jsx = reactJsxDevModule.jsxDEV;

                const module = await import(componentUrl);
                const Component = module.default || module;

                const root = createRoot(container);
                root.render(React.createElement(Component, {
                    ...props,
                    vsClick: (payload) => this.emit(host, 'click', payload),
                    vsChange: (payload) => this.emit(host, 'change', payload)
                }));

                const bridge = { root, shadow, container, React };
                this.bridges.set(host, bridge);
                return bridge;
            } catch (e) {
                console.error('React Bridge 挂载失败:', e);
                return null;
            }
        },

        unmount(host) {
            const bridge = this.bridges.get(host);
            if (bridge) {
                bridge.root.unmount();
                this.bridges.delete(host);
            }
        },

        emit(host, event, payload) {
            host.dispatchEvent(new CustomEvent(`voa:${event}`, { detail: payload }));
        }
    };

    // #endregion

    // #region WASM 模块注册表

    const moduleState = {
        UNREGISTERED: 'unregistered',
        LOADING: 'loading',
        LOADED: 'loaded',
        ERROR: 'error',
        UNLOADED: 'unloaded'
    };

    const moduleRegistry = new Map();
    const moduleInstances = new Map();

    function registerModule(name, config) {
        moduleRegistry.set(name, {
            name,
            wasmUrl: config.wasmUrl || (name + '.wasm'),
            glueUrl: config.glueUrl || null,
            imports: config.imports || {},
            memory: config.memory || null,
            onLoad: config.onLoad || null,
            onError: config.onError || null,
            state: moduleState.UNREGISTERED,
        });
    }

    async function loadModule(name) {
        if (moduleInstances.has(name)) {
            var existing = moduleInstances.get(name);
            if (existing.state === moduleState.LOADED) {
                return existing;
            }
        }

        const config = moduleRegistry.get(name);
        if (!config) {
            throw new Error('模块未注册: ' + name);
        }

        config.state = moduleState.LOADING;

        try {
            var memory = config.memory || new WebAssembly.Memory({ initial: 256 });

            var glue = {};
            if (config.glueUrl) {
                try {
                    var glueModule = await import(config.glueUrl);
                    glue = glueModule.default || glueModule;
                } catch (e) {
                    console.warn('胶水加载失败 ' + config.glueUrl + ':', e);
                }
            }

            var importObject = Object.assign({},
                config.imports,
                { env: Object.assign({ memory: memory }, glue.imports || {}) }
            );

            var exports = await loadWasm(config.wasmUrl, importObject);

            var wasmExports = {};
            if (exports) {
                Object.keys(exports).forEach(function(key) {
                    if (typeof exports[key] === 'function') {
                        wasmExports[key] = exports[key];
                    }
                });
            }

            var mod = {
                name: name,
                memory: memory,
                exports: wasmExports,
                glue: glue,
                state: moduleState.LOADED,
            };

            moduleInstances.set(name, mod);
            config.state = moduleState.LOADED;

            if (glue.onReady) {
                glue.onReady(exports, memory);
            }

            if (config.onLoad) {
                config.onLoad(mod);
            }

            return mod;
        } catch (e) {
            config.state = moduleState.ERROR;
            if (config.onError) {
                config.onError(e);
            }
            throw e;
        }
    }

    function getModule(name) {
        return moduleInstances.get(name) || null;
    }

    function unloadModule(name) {
        var config = moduleRegistry.get(name);
        var mod = moduleInstances.get(name);

        if (mod && mod.glue && mod.glue.onUnload) {
            try {
                mod.glue.onUnload();
            } catch (e) {
                console.warn('模块卸载回调失败 ' + name + ':', e);
            }
        }

        if (config) {
            config.state = moduleState.UNLOADED;
        }

        moduleInstances.delete(name);

        if (mod && mod.memory) {
            mod.memory = null;
            mod.exports = null;
        }
    }

    function callWasmExport(moduleName, funcName) {
        var args = Array.prototype.slice.call(arguments, 2);
        var mod = moduleInstances.get(moduleName);

        if (!mod || mod.state !== moduleState.LOADED) {
            throw new Error('模块未加载: ' + moduleName);
        }

        if (!mod.exports || typeof mod.exports[funcName] !== 'function') {
            throw new Error('WASM 导出未找到: ' + moduleName + '.' + funcName);
        }

        try {
            return mod.exports[funcName].apply(null, args);
        } catch (e) {
            console.error('WASM 调用失败 ' + moduleName + '.' + funcName + ':', e);
            throw e;
        }
    }

    function getModuleState(name) {
        var config = moduleRegistry.get(name);
        return config ? config.state : moduleState.UNREGISTERED;
    }

    function isModuleLoaded(name) {
        var mod = moduleInstances.get(name);
        return mod !== undefined && mod.state === moduleState.LOADED;
    }

    // #endregion

    // #region 字符串编组（WASM ↔ JS 共享内存）

    function readString(memory, ptr, len) {
        if (!memory) return '';
        var bytes = new Uint8Array(memory.buffer, ptr, len);
        return new TextDecoder().decode(bytes);
    }

    function allocString(memory, allocFn, str) {
        if (!memory || !allocFn) return 0;
        var encoder = new TextEncoder();
        var bytes = encoder.encode(str);
        var ptr = allocFn(bytes.length);
        new Uint8Array(memory.buffer).set(bytes, ptr);
        return (ptr << 16) | bytes.length;
    }

    function freeString(memory, freeFn, packed) {
        if (!memory || !freeFn) return;
        var ptr = packed >> 16;
        var len = packed & 0xffff;
        if (ptr > 0) {
            freeFn(ptr, len);
        }
    }

    // #endregion

    // #region DOM 句柄表

    var domHandles = [null];

    function storeDomHandle(el) {
        var id = domHandles.length;
        domHandles.push(el);
        return id;
    }

    function getDomHandle(id) {
        return domHandles[id] || null;
    }

    function releaseDomHandle(id) {
        if (id > 0 && id < domHandles.length) {
            domHandles[id] = null;
        }
    }

    // #endregion

    // #region WASM 加载

    async function loadWasm(wasmUrl, imports) {
        var importObject = imports || {
            env: {
                memory: new WebAssembly.Memory({ initial: 256 }),
            }
        };

        var useStreaming = typeof WebAssembly.instantiateStreaming === 'function';

        if (useStreaming) {
            var response = await fetch(wasmUrl);
            if (!response.ok) {
                throw new Error('WASM 加载失败: ' + response.status + ' ' + response.statusText);
            }
            var result = await WebAssembly.instantiateStreaming(response, importObject);
            return result.instance.exports;
        }

        var fallbackResponse = await fetch(wasmUrl);
        var buffer = await fallbackResponse.arrayBuffer();
        var fallbackResult = await WebAssembly.instantiate(buffer, importObject);
        return fallbackResult.instance.exports;
    }

    var wasmWorker = null;

    async function loadWasmWithWorker(wasmUrl, imports) {
        if (!wasmWorker) {
            wasmWorker = new Worker('/wasm-compiler-worker.js');
        }

        var importObject = imports || {
            env: {
                memory: new WebAssembly.Memory({ initial: 256 }),
            }
        };

        return new Promise(function(resolve, reject) {
            wasmWorker.onmessage = async function(e) {
                if (e.data.type === 'ready') {
                    wasmWorker.postMessage({ type: 'compile', url: wasmUrl, imports: importObject });
                } else if (e.data.type === 'module') {
                    var result = await WebAssembly.instantiate(e.data.module, importObject);
                    resolve(result.instance.exports);
                } else if (e.data.type === 'error') {
                    reject(new Error(e.data.error));
                }
            };

            wasmWorker.onerror = function(e) {
                reject(new Error('Worker 错误: ' + e.message));
            };

            wasmWorker.postMessage({ type: 'init' });
        });
    }

    // #endregion

    // #region 应用启动

    function boot(appName, config) {
        var cfg = config || {};
        registerModule(appName, {
            wasmUrl: cfg.wasmUrl || (appName + '.wasm'),
            glueUrl: cfg.glueUrl || null,
            imports: cfg.imports || {},
            memory: cfg.memory || null,
            onLoad: cfg.onLoad || null,
        });

        if (cfg.appUrl) {
            var script = document.createElement('script');
            script.src = cfg.appUrl;
            script.onload = function() {
                if (cfg.autoInit !== false) {
                    loadModule(appName).catch(function(e) {
                        console.error('模块启动失败 ' + appName + ':', e);
                    });
                }
            };
            document.head.appendChild(script);
        }
    }

    function autoBoot() {
        var scripts = document.querySelectorAll('script[data-voa-app]');
        scripts.forEach(function(script) {
            var appName = script.dataset.voaApp;
            var config = {};

            if (script.dataset.wasmUrl) config.wasmUrl = script.dataset.wasmUrl;
            if (script.dataset.glueUrl) config.glueUrl = script.dataset.glueUrl;
            if (script.dataset.appUrl) config.appUrl = script.dataset.appUrl;
            if (script.dataset.autoInit === 'false') config.autoInit = false;

            boot(appName, config);
        });
    }

    if (typeof document !== 'undefined') {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', autoBoot);
        } else {
            autoBoot();
        }
    }

    // #endregion

    // #region 客户端路由

    function createRouter(routes, options) {
        const [getPath, setPath] = createSignal(window.location.pathname);
        const [getParams, setParams] = createSignal({});

        function parseRoute(pattern) {
            const names = [];
            const regexStr = pattern.replace(/:([^/]+)/g, function(_, name) {
                names.push(name);
                return '([^/]+)';
            });
            return { regex: new RegExp('^' + regexStr + '$'), names: names };
        }

        const parsedRoutes = routes.map(function(route) {
            return { pattern: route.path, parsed: parseRoute(route.path), component: route.component };
        });

        function matchRoute(path) {
            for (var i = 0; i < parsedRoutes.length; i++) {
                var r = parsedRoutes[i];
                var m = path.match(r.parsed.regex);
                if (m) {
                    var params = {};
                    for (var j = 0; j < r.parsed.names.length; j++) {
                        params[r.parsed.names[j]] = m[j + 1];
                    }
                    return { route: r, params: params };
                }
            }
            return null;
        }

        function navigate(path) {
            if (path === getPath()) return;
            window.history.pushState({}, '', path);
            setPath(path);
            var match = matchRoute(path);
            if (match) {
                setParams(match.params);
            }
        }

        function handlePopState() {
            setPath(window.location.pathname);
            var match = matchRoute(window.location.pathname);
            if (match) {
                setParams(match.params);
            }
        }

        window.addEventListener('popstate', handlePopState);

        onCleanup(function() {
            window.removeEventListener('popstate', handlePopState);
        });

        return { getPath: getPath, getParams: getParams, navigate: navigate, matchRoute: matchRoute };
    }

    function Link(props) {
        var el = createElement('a');
        setAttribute(el, 'href', props.href || '#');
        el.addEventListener('click', function(e) {
            e.preventDefault();
            if (props.navigate) {
                props.navigate(props.href);
            }
        });
        if (props.children) {
            if (Array.isArray(props.children)) {
                props.children.forEach(function(child) { insertNode(el, child); });
            } else {
                insertNode(el, props.children);
            }
        }
        return el;
    }

    function RouterView(router) {
        var match = router.matchRoute(router.getPath());
        if (match && match.route.component) {
            return createComponent(match.route.component, { params: match.params });
        }
        var el = createElement('div');
        setAttribute(el, 'class', 'voa-404');
        var text = createTextNode('页面未找到');
        insertNode(el, text);
        return el;
    }

    // #endregion

    // #region 导出

    const Voa = {
        // 响应式
        createSignal,
        createEffect,
        createMemo,
        batch,
        untrack,
        // 生命周期
        onMount,
        onCleanup,
        // 挂载
        mount,
        mountIsland,
        unmountIsland,
        // 模板
        h,
        Fragment,
        createElement,
        createTextNode,
        dynamicText,
        dynamicAttribute,
        conditional,
        listMap,
        insertNode,
        removeNode,
        setAttribute,
        setProperty,
        createComponent,
        // Islands
        IslandStrategy,
        registerIsland,
        getIslandConfig,
        getAllIslands,
        hydrateIslands,
        createIslandElement,
        getScheduler,
        // Vue/React Bridge
        VueBridge,
        ReactBridge,
        // WASM 模块管理
        registerModule,
        loadModule,
        getModule,
        unloadModule,
        callWasmExport,
        getModuleState,
        isModuleLoaded,
        loadWasm,
        loadWasmWithWorker,
        // 字符串编组
        readString,
        allocString,
        freeString,
        // 模块状态枚举
        ModuleState: moduleState,
        // DOM 句柄
        storeDomHandle,
        getDomHandle,
        releaseDomHandle,
        // 应用启动
        boot,
        // 路由
        createRouter,
        Link,
        RouterView,
    };

    global.Voa = Voa;

    // #endregion

})(typeof window !== 'undefined' ? window : globalThis);
