using System.Text;

namespace Asgard.CLI.Compiler;

/// <summary>
///     PWA Service Worker 与 Manifest 生成器
///     支持多策略缓存（预缓存/网络优先/缓存优先）、更新检测、安装提示、离线回退
/// </summary>
public static class PwaGenerator
{
    /// <summary>
    ///     生成 Service Worker 脚本，包含安装/激活/抓取/消息事件
    /// </summary>
    public static string GenerateServiceWorker(string cacheName, string[] assets)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// VOA PWA Service Worker — 自动生成");
        sb.AppendLine($"const CACHE_PREFIX = '{cacheName}';");
        sb.AppendLine($"const CACHE_VERSION = 'v1';");
        sb.AppendLine($"const CACHE_NAME = CACHE_PREFIX + '-' + CACHE_VERSION;");
        sb.AppendLine();
        sb.AppendLine("const PRECACHE_URLS = [");

        foreach (var asset in assets)
        {
            sb.AppendLine($"  '{asset}',");
        }

        sb.AppendLine("];");
        sb.AppendLine();
        sb.AppendLine("const RUNTIME_CACHE_NAME = CACHE_PREFIX + '-runtime';");
        sb.AppendLine();
        sb.AppendLine("// 需要网络优先策略的动态资源模式");
        sb.AppendLine("const NETWORK_FIRST_PATTERNS = [");
        sb.AppendLine("  /^\\/api\\//,");
        sb.AppendLine("  /\\/data\\//,");
        sb.AppendLine("];");
        sb.AppendLine();
        sb.AppendLine("// #region install — 预缓存静态资源");
        sb.AppendLine("self.addEventListener('install', function(event) {");
        sb.AppendLine("  event.waitUntil(");
        sb.AppendLine("    caches.open(CACHE_NAME)");
        sb.AppendLine("      .then(function(cache) {");
        sb.AppendLine("        console.log('[VOA SW] 预缓存 ' + PRECACHE_URLS.length + ' 个资源');");
        sb.AppendLine("        return cache.addAll(PRECACHE_URLS);");
        sb.AppendLine("      })");
        sb.AppendLine("      .then(function() {");
        sb.AppendLine("        return self.skipWaiting();");
        sb.AppendLine("      })");
        sb.AppendLine("  );");
        sb.AppendLine("});");
        sb.AppendLine("// #endregion");
        sb.AppendLine();
        sb.AppendLine("// #region activate — 清理旧缓存 + 通知客户端");
        sb.AppendLine("self.addEventListener('activate', function(event) {");
        sb.AppendLine("  event.waitUntil(");
        sb.AppendLine("    caches.keys().then(function(names) {");
        sb.AppendLine("      return Promise.all(");
        sb.AppendLine("        names.filter(function(name) {");
        sb.AppendLine("          return name.startsWith(CACHE_PREFIX) && name !== CACHE_NAME && name !== RUNTIME_CACHE_NAME;");
        sb.AppendLine("        }).map(function(name) {");
        sb.AppendLine("          console.log('[VOA SW] 删除过期缓存:', name);");
        sb.AppendLine("          return caches.delete(name);");
        sb.AppendLine("        })");
        sb.AppendLine("      );");
        sb.AppendLine("    }).then(function() {");
        sb.AppendLine("      return self.clients.claim();");
        sb.AppendLine("    })");
        sb.AppendLine("  );");
        sb.AppendLine("});");
        sb.AppendLine("// #endregion");
        sb.AppendLine();
        sb.AppendLine("// #region fetch — 多策略缓存路由");
        sb.AppendLine("self.addEventListener('fetch', function(event) {");
        sb.AppendLine("  if (event.request.method !== 'GET') return;");
        sb.AppendLine();
        sb.AppendLine("  var url = new URL(event.request.url);");
        sb.AppendLine();
        sb.AppendLine("  // 跳过 chrome-extension 和非 http(s) 协议");
        sb.AppendLine("  if (url.protocol !== 'http:' && url.protocol !== 'https:') return;");
        sb.AppendLine();
        sb.AppendLine("  var useNetworkFirst = NETWORK_FIRST_PATTERNS.some(function(p) {");
        sb.AppendLine("    return p.test(event.request.url);");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  if (useNetworkFirst) {");
        sb.AppendLine("    event.respondWith(networkFirstStrategy(event.request));");
        sb.AppendLine("  } else {");
        sb.AppendLine("    event.respondWith(cacheFirstStaleRevalidate(event.request));");
        sb.AppendLine("  }");
        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("function cacheFirstStaleRevalidate(request) {");
        sb.AppendLine("  return caches.match(request).then(function(cached) {");
        sb.AppendLine("    var networkFetch = fetch(request).then(function(response) {");
        sb.AppendLine("      if (response && response.status === 200) {");
        sb.AppendLine("        var clone = response.clone();");
        sb.AppendLine("        caches.open(RUNTIME_CACHE_NAME).then(function(cache) {");
        sb.AppendLine("          cache.put(request, clone);");
        sb.AppendLine("        });");
        sb.AppendLine("      }");
        sb.AppendLine("      return response;");
        sb.AppendLine("    }).catch(function() {");
        sb.AppendLine("      return null;");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    return cached || networkFetch || caches.match('/offline.html');");
        sb.AppendLine("  });");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("function networkFirstStrategy(request) {");
        sb.AppendLine("  return fetch(request).then(function(response) {");
        sb.AppendLine("    if (response && response.status === 200) {");
        sb.AppendLine("      var clone = response.clone();");
        sb.AppendLine("      caches.open(RUNTIME_CACHE_NAME).then(function(cache) {");
        sb.AppendLine("        cache.put(request, clone);");
        sb.AppendLine("      });");
        sb.AppendLine("    }");
        sb.AppendLine("    return response;");
        sb.AppendLine("  }).catch(function() {");
        sb.AppendLine("    return caches.match(request).then(function(cached) {");
        sb.AppendLine("      return cached || caches.match('/offline.html');");
        sb.AppendLine("    });");
        sb.AppendLine("  });");
        sb.AppendLine("}");
        sb.AppendLine("// #endregion");
        sb.AppendLine();
        sb.AppendLine("// #region message — 客户端通信（跳过等待 + 缓存清理）");
        sb.AppendLine("self.addEventListener('message', function(event) {");
        sb.AppendLine("  if (event.data && event.data.type === 'SKIP_WAITING') {");
        sb.AppendLine("    self.skipWaiting();");
        sb.AppendLine("  }");
        sb.AppendLine("  if (event.data && event.data.type === 'CLEAR_RUNTIME') {");
        sb.AppendLine("    caches.delete(RUNTIME_CACHE_NAME).then(function() {");
        sb.AppendLine("      console.log('[VOA SW] 运行时缓存已清理');");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine("});");
        sb.AppendLine("// #endregion");

        return sb.ToString();
    }

    /// <summary>
    ///     生成 Web App Manifest，包含完整的图标、启动和分享配置
    /// </summary>
    public static string GenerateManifest(string name, string shortName,
        string themeColor = "#7c3aed", string backgroundColor = "#0f0f23")
    {
        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine($"  \"name\": \"{name}\",");
        sb.AppendLine($"  \"short_name\": \"{shortName}\",");
        sb.AppendLine("  \"start_url\": \"/\",");
        sb.AppendLine("  \"scope\": \"/\",");
        sb.AppendLine("  \"display\": \"standalone\",");
        sb.AppendLine("  \"orientation\": \"any\",");
        sb.AppendLine("  \"lang\": \"zh-CN\",");
        sb.AppendLine("  \"dir\": \"ltr\",");
        sb.AppendLine($"  \"background_color\": \"{backgroundColor}\",");
        sb.AppendLine($"  \"theme_color\": \"{themeColor}\",");
        sb.AppendLine($"  \"description\": \"{name} — 基于 VOA 全栈框架构建的渐进式 Web 应用\",");
        sb.AppendLine("  \"categories\": [\"productivity\", \"utilities\"],");
        sb.AppendLine("  \"prefer_related_applications\": false,");
        sb.AppendLine("  \"icons\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-72.png\",");
        sb.AppendLine("      \"sizes\": \"72x72\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-96.png\",");
        sb.AppendLine("      \"sizes\": \"96x96\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-128.png\",");
        sb.AppendLine("      \"sizes\": \"128x128\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-144.png\",");
        sb.AppendLine("      \"sizes\": \"144x144\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-152.png\",");
        sb.AppendLine("      \"sizes\": \"152x152\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-192.png\",");
        sb.AppendLine("      \"sizes\": \"192x192\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-384.png\",");
        sb.AppendLine("      \"sizes\": \"384x384\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon-512.png\",");
        sb.AppendLine("      \"sizes\": \"512x512\",");
        sb.AppendLine("      \"type\": \"image/png\"");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"src\": \"/icons/icon.svg\",");
        sb.AppendLine("      \"sizes\": \"any\",");
        sb.AppendLine("      \"type\": \"image/svg+xml\",");
        sb.AppendLine("      \"purpose\": \"any maskable\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"screenshots\": [],");
        sb.AppendLine("  \"share_target\": {");
        sb.AppendLine("    \"action\": \"/share\",");
        sb.AppendLine("    \"method\": \"GET\",");
        sb.AppendLine("    \"params\": {");
        sb.AppendLine("      \"title\": \"title\",");
        sb.AppendLine("      \"text\": \"text\",");
        sb.AppendLine("      \"url\": \"url\"");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    ///     生成 PWA 注册脚本，注入到 HTML 中
    ///     包含 Service Worker 注册、更新检测、安装提示
    /// </summary>
    public static string GenerateRegistrationScript()
    {
        var sb = new StringBuilder();

        sb.AppendLine("<script>");
        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();
        sb.AppendLine("  var isStandalone = window.matchMedia('(display-mode: standalone)').matches;");
        sb.AppendLine("  var deferredPrompt = null;");
        sb.AppendLine();
        sb.AppendLine("  // Service Worker 注册 + 更新检测");
        sb.AppendLine("  if ('serviceWorker' in navigator) {");
        sb.AppendLine("    window.addEventListener('load', function() {");
        sb.AppendLine("      navigator.serviceWorker.register('/sw.js')");
        sb.AppendLine("        .then(function(registration) {");
        sb.AppendLine("          console.log('[VOA PWA] Service Worker 已注册', registration.scope);");
        sb.AppendLine();
        sb.AppendLine("          registration.addEventListener('updatefound', function() {");
        sb.AppendLine("            var newWorker = registration.installing;");
        sb.AppendLine("            if (!newWorker) return;");
        sb.AppendLine();
        sb.AppendLine("            newWorker.addEventListener('statechange', function() {");
        sb.AppendLine("              if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {");
        sb.AppendLine("                showUpdateBanner(registration);");
        sb.AppendLine("              }");
        sb.AppendLine("            });");
        sb.AppendLine("          });");
        sb.AppendLine("        })");
        sb.AppendLine("        .catch(function(err) {");
        sb.AppendLine("          console.warn('[VOA PWA] SW 注册失败:', err.message);");
        sb.AppendLine("        });");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  // 安装提示拦截");
        sb.AppendLine("  window.addEventListener('beforeinstallprompt', function(e) {");
        sb.AppendLine("    e.preventDefault();");
        sb.AppendLine("    deferredPrompt = e;");
        sb.AppendLine();
        sb.AppendLine("    if (!isStandalone) {");
        sb.AppendLine("      showInstallBanner();");
        sb.AppendLine("    }");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  // 已安装检测");
        sb.AppendLine("  window.addEventListener('appinstalled', function() {");
        sb.AppendLine("    deferredPrompt = null;");
        sb.AppendLine("    console.log('[VOA PWA] 应用已安装');");
        sb.AppendLine("    hideInstallBanner();");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  // 更新条幅");
        sb.AppendLine("  function showUpdateBanner(registration) {");
        sb.AppendLine("    var banner = document.createElement('div');");
        sb.AppendLine("    banner.className = 'voa-pwa-update';");
        sb.AppendLine("    banner.innerHTML = [");
        sb.AppendLine("      '<div style=\"position:fixed;bottom:16px;left:50%;transform:translateX(-50%);\" +");
        sb.AppendLine("        'background:#7c3aed;color:#fff;padding:12px 24px;border-radius:8px;\" +");
        sb.AppendLine("        'z-index:99999;box-shadow:0 4px 24px rgba(124,58,237,0.4);\" +");
        sb.AppendLine("        'font-family:system-ui,sans-serif;font-size:14px>\">',");
        sb.AppendLine("      '<span>新版本可用</span>'");
        sb.AppendLine("      '<button onclick=\"location.reload()\" ' +");
        sb.AppendLine("        'style=\"margin-left:12px;background:#fff;color:#7c3aed;border:none;\" +");
        sb.AppendLine("        'padding:4px 12px;border-radius:4px;cursor:pointer;font-weight:600\">立即更新</button>',");
        sb.AppendLine("      '</div>'");
        sb.AppendLine("    ].join('');");
        sb.AppendLine("    document.body.appendChild(banner);");
        sb.AppendLine();
        sb.AppendLine("    setTimeout(function() {");
        sb.AppendLine("      if (banner.parentNode) banner.parentNode.removeChild(banner);");
        sb.AppendLine("    }, 30000);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  // 安装条幅");
        sb.AppendLine("  function showInstallBanner() {");
        sb.AppendLine("    var existing = document.querySelector('.voa-pwa-install');");
        sb.AppendLine("    if (existing) return;");
        sb.AppendLine();
        sb.AppendLine("    var banner = document.createElement('div');");
        sb.AppendLine("    banner.className = 'voa-pwa-install';");
        sb.AppendLine("    banner.innerHTML = [");
        sb.AppendLine("      '<div style=\"position:fixed;bottom:80px;left:50%;transform:translateX(-50%);background:#1a1a2e;color:#e0e0ff;padding:16px 24px;border-radius:12px;z-index:99998;box-shadow:0 4px 24px rgba(0,0,0,0.3);font-family:system-ui,sans-serif;font-size:14px;border:1px solid #7c3aed\">',");
        sb.AppendLine("      '<span>将此应用安装到桌面</span>',");
        sb.AppendLine("      '<button id=\"voa-install-btn\" style=\"margin-left:12px;background:#7c3aed;color:#fff;border:none;padding:6px 16px;border-radius:6px;cursor:pointer;font-weight:600\">安装</button>',");
        sb.AppendLine("      '<button onclick=\"this.parentElement.parentElement.remove()\" style=\"margin-left:8px;background:transparent;color:#888;border:none;padding:6px 8px;cursor:pointer\">✕</button>',");
        sb.AppendLine("      '</div>'");
        sb.AppendLine("    ].join('');");
        sb.AppendLine("    document.body.appendChild(banner);");
        sb.AppendLine();
        sb.AppendLine("    document.getElementById('voa-install-btn').addEventListener('click', function() {");
        sb.AppendLine("      if (deferredPrompt) {");
        sb.AppendLine("        deferredPrompt.prompt();");
        sb.AppendLine("        deferredPrompt.userChoice.then(function(choice) {");
        sb.AppendLine("          console.log('[VOA PWA] 用户选择:', choice.outcome);");
        sb.AppendLine("          deferredPrompt = null;");
        sb.AppendLine("        });");
        sb.AppendLine("      }");
        sb.AppendLine("      banner.remove();");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function hideInstallBanner() {");
        sb.AppendLine("    var el = document.querySelector('.voa-pwa-install');");
        sb.AppendLine("    if (el) el.remove();");
        sb.AppendLine("  }");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");

        return sb.ToString();
    }

    /// <summary>
    ///     生成离线回退页面 HTML
    /// </summary>
    public static string GenerateOfflinePage(string appName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{appName} — 离线</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("    body {");
        sb.AppendLine("      font-family: system-ui, -apple-system, sans-serif;");
        sb.AppendLine("      background: #0f0f23;");
        sb.AppendLine("      color: #e0e0ff;");
        sb.AppendLine("      min-height: 100vh;");
        sb.AppendLine("      display: flex;");
        sb.AppendLine("      flex-direction: column;");
        sb.AppendLine("      align-items: center;");
        sb.AppendLine("      justify-content: center;");
        sb.AppendLine("      text-align: center;");
        sb.AppendLine("      padding: 24px;");
        sb.AppendLine("    }");
        sb.AppendLine("    .icon {");
        sb.AppendLine("      width: 80px;");
        sb.AppendLine("      height: 80px;");
        sb.AppendLine("      border-radius: 20px;");
        sb.AppendLine("      background: linear-gradient(135deg, #7c3aed, #3b82f6);");
        sb.AppendLine("      display: flex;");
        sb.AppendLine("      align-items: center;");
        sb.AppendLine("      justify-content: center;");
        sb.AppendLine("      font-size: 40px;");
        sb.AppendLine("      margin-bottom: 24px;");
        sb.AppendLine("      box-shadow: 0 8px 32px rgba(124,58,237,0.3);");
        sb.AppendLine("    }");
        sb.AppendLine("    h1 { font-size: 24px; font-weight: 600; margin-bottom: 8px; }");
        sb.AppendLine("    p { color: #888; font-size: 14px; margin-bottom: 32px; max-width: 320px; line-height: 1.6; }");
        sb.AppendLine("    .retry {");
        sb.AppendLine("      background: #7c3aed;");
        sb.AppendLine("      color: #fff;");
        sb.AppendLine("      border: none;");
        sb.AppendLine("      padding: 10px 32px;");
        sb.AppendLine("      border-radius: 8px;");
        sb.AppendLine("      font-size: 14px;");
        sb.AppendLine("      font-weight: 600;");
        sb.AppendLine("      cursor: pointer;");
        sb.AppendLine("      transition: background 0.2s;");
        sb.AppendLine("    }");
        sb.AppendLine("    .retry:hover { background: #6d28d9; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"icon\">📡</div>");
        sb.AppendLine($"  <h1>{appName}</h1>");
        sb.AppendLine("  <p>您当前处于离线状态。请检查网络连接后重试。</p>");
        sb.AppendLine("  <button class=\"retry\" onclick=\"location.reload()\">重新连接</button>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}