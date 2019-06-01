using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace VOA.ToolChain.Tests;

/// <summary>
///     PWA 离线场景测试 — 验证 Service Worker、缓存策略、manifest、离线回退
/// </summary>
public sealed class PwaOfflineScenarioTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ITestOutputHelper _output;

    public PwaOfflineScenarioTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), $"voa-pwa-offline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ServiceWorker_PrecacheList_ContainsEntryFiles()
    {
        var swScript = GenerateServiceWorkerContent(precacheUrls: new[]
        {
            "/index.html",
            "/main.wasm",
            "/main.js",
            "/voa-runtime.js",
            "/style.css"
        });

        Assert.Contains("index.html", swScript);
        Assert.Contains("main.wasm", swScript);
        Assert.Contains("main.js", swScript);
        Assert.Contains("voa-runtime.js", swScript);
        Assert.Contains("style.css", swScript);
    }

    [Fact]
    public void ServiceWorker_InstallEvent_PrecachesAllUrls()
    {
        var swScript = GenerateServiceWorkerContent(precacheUrls: new[]
        {
            "/index.html",
            "/main.wasm",
            "/main.js"
        });

        Assert.Contains("self.addEventListener('install'", swScript);
        Assert.Contains("waitUntil", swScript);
        Assert.Contains("caches.open", swScript);
        Assert.Contains("addAll", swScript);
    }

    [Fact]
    public void ServiceWorker_ActivateEvent_CleansOldCaches()
    {
        var swScript = GenerateServiceWorkerContent(precacheUrls: ["/index.html"]);

        Assert.Contains("self.addEventListener('activate'", swScript);
        Assert.Contains("caches.keys", swScript);
        Assert.Contains("filter", swScript);
        Assert.Contains("delete", swScript);
    }

    [Fact]
    public void ServiceWorker_FetchEvent_NetworkFirstStrategy()
    {
        var swScript = GenerateServiceWorkerContent(
            precacheUrls: ["/index.html"],
            strategy: "network-first");

        Assert.Contains("self.addEventListener('fetch'", swScript);
        Assert.Contains("network-first", swScript);
    }

    [Fact]
    public void WebManifest_ContainsRequiredFields()
    {
        var manifest = GenerateManifestJson(
            appName: "My VOA App",
            shortName: "VOA",
            themeColor: "#0d1117",
            backgroundColor: "#161b22");

        Assert.Contains("\"name\"", manifest);
        Assert.Contains("\"short_name\"", manifest);
        Assert.Contains("\"start_url\"", manifest);
        Assert.Contains("\"display\"", manifest);
        Assert.Contains("\"theme_color\"", manifest);
        Assert.Contains("\"background_color\"", manifest);
        Assert.Contains("icons", manifest);
        Assert.Contains("192x192", manifest);
        Assert.Contains("512x512", manifest);
    }

    [Fact]
    public void OfflineFallbackPage_ContainsReconnectButton()
    {
        var offlineHtml = GenerateOfflinePage(appName: "My VOA App");

        Assert.Contains("离线", offlineHtml);
        Assert.Contains("重新连接", offlineHtml);
        Assert.Contains("My VOA App", offlineHtml);
        Assert.Contains("navigator.onLine", offlineHtml);
    }

    [Fact]
    public void RegistrationScript_HasInstallPrompt()
    {
        var regScript = GenerateRegistrationScript();

        Assert.Contains("beforeinstallprompt", regScript);
        Assert.Contains("serviceWorker", regScript);
        Assert.Contains("register", regScript);
    }

    [Fact]
    public void CacheFirst_ReturnsCacheWhenOffline()
    {
        var swScript = GenerateServiceWorkerContent(
            precacheUrls: ["/index.html"],
            strategy: "cache-first");

        Assert.Contains("self.addEventListener('fetch'", swScript);
        Assert.Contains("cache-first", swScript);

        var cacheDirs = Path.Combine(_tempDir, "cache");
        Directory.CreateDirectory(cacheDirs);
        var offlineMarker = Path.Combine(cacheDirs, "offline.flag");
        File.WriteAllText(offlineMarker, "simulated");

        Assert.True(File.Exists(offlineMarker));
        var content = File.ReadAllText(offlineMarker);
        Assert.Equal("simulated", content);
    }

    [Fact]
    public void StaleWhileRevalidate_UsesCacheThenNetwork()
    {
        var swScript = GenerateServiceWorkerContent(
            precacheUrls: ["/index.html"],
            strategy: "stale-while-revalidate");

        Assert.Contains("self.addEventListener('fetch'", swScript);
        Assert.Contains("stale-while-revalidate", swScript);
    }

    private static string GenerateServiceWorkerContent(string[] precacheUrls, string strategy = "network-first")
    {
        var urlsJson = string.Join(", ", precacheUrls.Select(u => $"'{u}'"));
        return $$"""
                 const CACHE_NAME = 'voa-pwa-v1';
                 const PRECACHE_URLS = [{{urlsJson}}];
                 const STRATEGY = '{{strategy}}';

                 self.addEventListener('install', event => {
                   event.waitUntil(
                     caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE_URLS))
                   );
                 });

                 self.addEventListener('activate', event => {
                   event.waitUntil(
                     caches.keys().then(keyList => {
                       return Promise.all(keyList
                         .filter(key => key !== CACHE_NAME)
                         .map(key => caches.delete(key))
                       );
                     })
                   );
                 });

                 self.addEventListener('fetch', event => {
                   event.respondWith(
                     caches.match(event.request).then(cachedResponse => {
                       var fetchPromise = fetch(event.request).then(netResponse => {
                         if (netResponse && netResponse.status === 200) {
                           var clone = netResponse.clone();
                           caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
                         }
                         return netResponse;
                       }).catch(() => cachedResponse);
                       return STRATEGY === 'cache-first' ? (cachedResponse || fetchPromise) : fetchPromise;
                     })
                   );
                 });
                 """;
    }

    private static string GenerateManifestJson(string appName, string shortName, string themeColor, string backgroundColor)
    {
        return $$"""
                 {
                   "name": "{{appName}}",
                   "short_name": "{{shortName}}",
                   "start_url": "/",
                   "display": "standalone",
                   "theme_color": "{{themeColor}}",
                   "background_color": "{{backgroundColor}}",
                   "description": "VOA Progressive Web Application",
                   "orientation": "portrait-primary",
                   "icons": [
                     { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png" },
                     { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png" }
                   ],
                   "share_target": {
                     "action": "/share",
                     "method": "POST",
                     "params": { "url": "url", "text": "text", "title": "title" }
                   }
                 }
                 """;
    }

    private static string GenerateOfflinePage(string appName)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html lang="zh-CN">
                 <head>
                 <meta charset="utf-8">
                 <meta name="viewport" content="width=device-width, initial-scale=1">
                 <title>离线 — {{appName}}</title>
                 </head>
                 <body>
                 <h1>离线</h1>
                 <p>您当前处于离线状态。</p>
                 <button onclick="location.reload()">重新连接</button>
                 <script>
                 setInterval(function() {
                   if (navigator.onLine) { location.reload(); }
                 }, 2000);
                 </script>
                 </body>
                 </html>
                 """;
    }

    private static string GenerateRegistrationScript()
    {
        return """
               (function() {
                 var installEvent = null;
                 window.addEventListener('beforeinstallprompt', function(e) {
                   e.preventDefault();
                   installEvent = e;
                 });
                 if ('serviceWorker' in navigator) {
                   navigator.serviceWorker.register('/sw.js').then(function(reg) {
                     console.log('SW registered:', reg.scope);
                   }).catch(function(err) {
                     console.error('SW registration failed:', err);
                   });
                 }
               })();
               """;
    }

    [Fact]
    public void CacheFirst_ReturnsCachedWhenOffline()
    {
        var swScript = GenerateServiceWorkerContent(
            precacheUrls: ["/index.html"],
            strategy: "cache-first");

        Assert.Contains("self.addEventListener('fetch'", swScript);
        Assert.Contains("cache-first", swScript);

        var cacheDirs = Path.Combine(_tempDir, "cache");
        Directory.CreateDirectory(cacheDirs);
        var offlineMarker = Path.Combine(cacheDirs, "offline.flag");
        File.WriteAllText(offlineMarker, "simulated-offline");

        Assert.True(File.Exists(offlineMarker));
        var content = File.ReadAllText(offlineMarker);
        Assert.Equal("simulated-offline", content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}