using System.Text;
using Valkyrie.Runtime;
using Valkyrie.Runtime.Bridge;

namespace Asgard.CLI.Compiler;

/// <summary>
///     WASM 目标构建器，负责将编译结果整合为完整的 WASM 输出
///     包括：WASM 二进制、JS 桥接胶水、CSS、SSR 页面、PWA 资源
/// </summary>
public sealed class WasmTargetBuilder
{
    /// <summary>
    ///     构建 WASM 目标输出
    /// </summary>
    /// <param name="wasmResult">.v 文件编译产生的 WASM 结果</param>
    /// <param name="awslResults">.awsl 文件编译结果列表</param>
    /// <param name="moduleName">模块名</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="verbose">是否输出详细信息</param>
    /// <param name="pwa">是否生成 PWA 资源</param>
    /// <returns>构建结果</returns>
    public VoaBuildResult Build(WasmCompilationResult? wasmResult, List<AwslCompileResult> awslResults,
        string moduleName, string outputDir, bool verbose, bool pwa, bool ssr = false)
    {
        Directory.CreateDirectory(outputDir);

        var result = new VoaBuildResult();

        if (wasmResult?.WasmBytes is { } wasmBytes)
        {
            var wasmPath = Path.Combine(outputDir, $"{moduleName}.wasm");
            File.WriteAllBytes(wasmPath, wasmBytes);

            if (verbose)
            {
                Console.WriteLine($"  写入 {wasmPath} ({wasmBytes.Length} bytes)");
            }

            result.OutputFiles.Add(wasmPath);
        }

        var jsBridge = GenerateJsBridge(wasmResult?.JsImports, moduleName);
        var jsPath = Path.Combine(outputDir, $"{moduleName}.js");
        File.WriteAllText(jsPath, jsBridge, Encoding.UTF8);

        if (verbose)
        {
            Console.WriteLine($"  写入 {jsPath}");
        }

        result.OutputFiles.Add(jsPath);

        var css = MergeCss(awslResults);
        if (css.Length > 0)
        {
            var cssPath = Path.Combine(outputDir, $"{moduleName}.css");
            File.WriteAllText(cssPath, css, Encoding.UTF8);

            if (verbose)
            {
                Console.WriteLine($"  写入 {cssPath}");
            }

            result.OutputFiles.Add(cssPath);
        }

        CopyRuntimeJs(outputDir, verbose, result);

        var indexHtmlPath = Path.Combine(outputDir, "index.html");
        var html = GenerateIndexHtml(moduleName, awslResults, pwa, ssr);
        File.WriteAllText(indexHtmlPath, html, Encoding.UTF8);

        if (verbose)
        {
            Console.WriteLine($"  写入 {indexHtmlPath}");
        }

        result.OutputFiles.Add(indexHtmlPath);

        if (pwa)
        {
            GeneratePwaAssets(outputDir, moduleName, awslResults, verbose, result);
        }

        if (verbose)
        {
            Console.WriteLine($"VOA 构建完成，共生成 {result.OutputFiles.Count} 个文件");
        }

        return result;
    }

    /// <summary>
    ///     为 131 个 [js_builtin] 桥接函数声明生成 JS 桥接胶水代码
    /// </summary>
    private static string GenerateJsBridge(List<JsImportInfo>? jsImports, string moduleName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// VOA WASM Bridge — 自动生成，请勿手动编辑");
        sb.AppendLine($"(function() {{");
        sb.AppendLine($"  'use strict';");
        sb.AppendLine();
        sb.AppendLine($"  const moduleName = '{moduleName}';");

        if (jsImports is { Count: > 0 })
        {
            var bridgeCode = JsBridgeGenerator.GenerateBridgeCode(jsImports);
            sb.AppendLine(bridgeCode);

            EmitBuiltinValidation(sb, jsImports);
        }

        sb.AppendLine();
        sb.AppendLine($"  window['__voa_{moduleName}_ready__'] = true;");
        sb.AppendLine($"}}  )();");
        return sb.ToString();
    }

    private static void EmitBuiltinValidation(StringBuilder sb, List<JsImportInfo> imports)
    {
        var builtins = imports.Where(i => i.IsBuiltin).ToList();
        if (builtins.Count == 0) return;

        sb.AppendLine();
        sb.AppendLine($"  // 验证 {builtins.Count} 个 [js_builtin] 桥接目标");
        sb.AppendLine($"  var _missingBuiltins = [];");

        foreach (var b in builtins)
        {
            if (b.ModuleName == "document")
            {
                sb.AppendLine($"  if (typeof document.{b.FuncName} !== 'function') _missingBuiltins.push('document.{b.FuncName}');");
            }
            else if (b.ModuleName == "Element")
            {
                sb.AppendLine($"  if (typeof Element !== 'function' || typeof Element.prototype.{b.FuncName} !== 'function') _missingBuiltins.push('Element.{b.FuncName}');");
            }
            else if (b.ModuleName == "window")
            {
                sb.AppendLine($"  if (typeof {b.FuncName} !== 'function') _missingBuiltins.push('window.{b.FuncName}');");
            }
            else
            {
                sb.AppendLine($"  // 外部模块 '{b.ModuleName}.{b.FuncName}' 在运行时动态加载");
            }
        }

        sb.AppendLine($"  if (_missingBuiltins.length > 0) {{");
        sb.AppendLine($"    console.warn('VOA Bridge 缺失桥接:', _missingBuiltins);");
        sb.AppendLine($"  }}");
    }

    /// <summary>
    ///     合并所有 AWSL 组件生成的 CSS
    /// </summary>
    private static string MergeCss(List<AwslCompileResult> awslResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* VOA 组件样式 — 自动生成 */");

        foreach (var r in awslResults)
        {
            if (!string.IsNullOrWhiteSpace(r.Css))
            {
                sb.AppendLine();
                sb.AppendLine($"/* {r.ComponentName} */");
                sb.Append(r.Css);

                if (!r.Css.EndsWith('\n'))
                {
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     从 runtime/ 目录复制 voa-runtime.js 到输出目录
    /// </summary>
    private static void CopyRuntimeJs(string outputDir, bool verbose, VoaBuildResult result)
    {
        var runtimeDir = FindRuntimeDir();
        if (runtimeDir is null)
        {
            if (verbose)
            {
                Console.WriteLine("  警告：未找到 runtime/ 目录，跳过 voa-runtime.js 复制");
            }

            return;
        }

        var srcPath = Path.Combine(runtimeDir, "voa-runtime.js");
        if (!File.Exists(srcPath))
        {
            if (verbose)
            {
                Console.WriteLine($"  警告：未找到 {srcPath}");
            }

            return;
        }

        var destPath = Path.Combine(outputDir, "voa-runtime.js");
        File.Copy(srcPath, destPath, overwrite: true);

        if (verbose)
        {
            Console.WriteLine($"  复制 {destPath}");
        }

        result.OutputFiles.Add(destPath);
    }

    /// <summary>
    ///     查找 VOA 项目的 runtime/ 目录
    /// </summary>
    private static string? FindRuntimeDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var runtimePath = Path.Combine(dir.FullName, "runtime");
            if (Directory.Exists(runtimePath))
            {
                return runtimePath;
            }

            var parentRuntime = Path.Combine(dir.FullName, "..", "runtime");
            if (Directory.Exists(parentRuntime))
            {
                return Path.GetFullPath(parentRuntime);
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     生成 index.html，包含所有 AWSL 组件的 SSR 渲染内容
    /// </summary>
    private static string GenerateIndexHtml(string moduleName, List<AwslCompileResult> awslResults, bool pwa, bool ssr)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"zh-CN\">");
        sb.AppendLine($"<head>");
        sb.AppendLine($"  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{moduleName}</title>");
        sb.AppendLine($"  <link rel=\"stylesheet\" href=\"/{moduleName}.css\">");
        sb.AppendLine($"  <script src=\"/voa-runtime.js\"></script>");

        if (pwa)
        {
            sb.AppendLine($"  <link rel=\"manifest\" href=\"/manifest.json\">");
        }

        if (ssr)
        {
            sb.AppendLine($"  <script>window.__VOA_SSR__ = true;</script>");
        }

        sb.AppendLine($"</head>");
        sb.AppendLine($"<body>");
        sb.AppendLine($"  <div id=\"app\">");

        foreach (var r in awslResults)
        {
            var islandAttr = r.IslandType is not null
                ? $" data-island=\"{r.IslandType}\" data-component=\"{r.ComponentName}\""
                : $" data-component=\"{r.ComponentName}\"";

            if (r.HydrateStrategy is not null)
            {
                islandAttr += $" data-hydrate=\"{r.HydrateStrategy}\"";
            }

            sb.AppendLine($"    <div{islandAttr}></div>");
        }

        sb.AppendLine($"  </div>");

        sb.AppendLine($"  <script src=\"/{moduleName}.js\"></script>");

        sb.AppendLine($"  <script>");
        sb.AppendLine($"    document.addEventListener('DOMContentLoaded', function() {{");
        sb.AppendLine($"      if (Voa && Voa.hydrateIslands) {{");
        sb.AppendLine($"        Voa.hydrateIslands();");
        sb.AppendLine($"      }}");
        sb.AppendLine($"    }});");
        sb.AppendLine($"  </script>");

        if (pwa)
        {
            sb.Append(PwaGenerator.GenerateRegistrationScript());
            sb.AppendLine();
        }

        if (ssr)
        {
            sb.AppendLine($"  <script>");
            sb.AppendLine($"    Voa.boot('{moduleName}', {{");
            sb.AppendLine($"      wasmUrl: '/{moduleName}.wasm',");
            sb.AppendLine($"      glueUrl: '/{moduleName}.js'");
            sb.AppendLine($"    }});");
            sb.AppendLine($"  </script>");
        }

        sb.AppendLine($"</body>");
        sb.AppendLine($"</html>");

        return sb.ToString();
    }

    /// <summary>
    ///     生成 PWA 相关的 Service Worker 和 manifest.json
    /// </summary>
    private static void GeneratePwaAssets(string outputDir, string moduleName, List<AwslCompileResult> awslResults,
        bool verbose, VoaBuildResult result)
    {
        var assets = new List<string> { "/", "/index.html", $"/{moduleName}.js" };

        if (result.OutputFiles.Count > 0)
        {
            assets.Add($"/{moduleName}.css");
        }

        assets.Add("/voa-runtime.js");

        var offlineContent = PwaGenerator.GenerateOfflinePage(moduleName);
        var offlinePath = Path.Combine(outputDir, "offline.html");
        File.WriteAllText(offlinePath, offlineContent, Encoding.UTF8);
        assets.Add("/offline.html");

        if (verbose)
        {
            Console.WriteLine($"  写入 {offlinePath} (离线回退页面)");
        }

        result.OutputFiles.Add(offlinePath);

        var swContent = PwaGenerator.GenerateServiceWorker(moduleName, assets.ToArray());
        var swPath = Path.Combine(outputDir, "sw.js");
        File.WriteAllText(swPath, swContent, Encoding.UTF8);

        if (verbose)
        {
            Console.WriteLine($"  写入 {swPath} (PWA Service Worker)");
        }

        result.OutputFiles.Add(swPath);

        var manifestContent = PwaGenerator.GenerateManifest(moduleName, moduleName.ToLowerInvariant());
        var manifestPath = Path.Combine(outputDir, "manifest.json");
        File.WriteAllText(manifestPath, manifestContent, Encoding.UTF8);

        if (verbose)
        {
            Console.WriteLine($"  写入 {manifestPath} (PWA Manifest)");
        }

        result.OutputFiles.Add(manifestPath);
    }
}