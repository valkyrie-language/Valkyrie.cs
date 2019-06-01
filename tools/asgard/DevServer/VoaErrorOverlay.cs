using System.Text;

namespace Asgard.CLI.DevServer;

/// <summary>
///     VOA 错误覆盖层，在开发模式下显示编译错误和运行时错误。
///     支持错误分类（语法/编译/运行时）、多错误列表、源码上下文、可关闭覆盖层。
/// </summary>
public sealed class VoaErrorOverlay
{
    /// <summary>
    ///     渲染错误页面 HTML
    /// </summary>
    public string RenderErrorPage(string error, string? filePath, int statusCode = 500)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>VOA Error {statusCode}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetErrorStyles());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"voa-error-overlay\">");
        sb.AppendLine("    <div class=\"voa-error-header\">");
        sb.AppendLine($"      <span class=\"voa-error-code\">{statusCode}</span>");
        sb.AppendLine("      <span class=\"voa-error-title\">VOA 开发服务器错误</span>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class=\"voa-error-body\">");

        if (!string.IsNullOrEmpty(filePath))
        {
            sb.AppendLine($"      <div class=\"voa-error-file\">{EscapeHtml(filePath)}</div>");
        }

        sb.AppendLine($"      <pre class=\"voa-error-message\">{EscapeHtml(error)}</pre>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    ///     渲染结构化错误页面（支持多错误、错误分类、源码上下文）
    /// </summary>
    public string RenderStructuredErrorPage(VoaErrorInfo errorInfo)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>VOA Error</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(GetErrorStyles());
        sb.AppendLine(GetStructuredErrorStyles());
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"voa-error-overlay\">");
        sb.AppendLine("    <div class=\"voa-error-header\">");
        sb.AppendLine($"      <span class=\"voa-error-badge voa-error-badge-{errorInfo.Severity}\">{errorInfo.SeverityLabel}</span>");
        sb.AppendLine($"      <span class=\"voa-error-title\">{EscapeHtml(errorInfo.Title)}</span>");
        sb.AppendLine("    </div>");

        if (!string.IsNullOrEmpty(errorInfo.FilePath))
        {
            sb.AppendLine("    <div class=\"voa-error-location\">");
            sb.AppendLine($"      <span class=\"voa-error-file\">{EscapeHtml(errorInfo.FilePath)}</span>");
            if (errorInfo.Line > 0)
            {
                sb.AppendLine($"      <span class=\"voa-error-line\">行 {errorInfo.Line}{(errorInfo.Column > 0 ? $", 列 {errorInfo.Column}" : "")}</span>");
            }

            sb.AppendLine("    </div>");
        }

        sb.AppendLine("    <div class=\"voa-error-body\">");
        sb.AppendLine($"      <pre class=\"voa-error-message\">{EscapeHtml(errorInfo.Message)}</pre>");

        if (errorInfo.SourceContext is not null)
        {
            sb.AppendLine("      <div class=\"voa-error-source\">");
            sb.AppendLine("        <div class=\"voa-source-header\">源码上下文</div>");
            sb.AppendLine("        <pre class=\"voa-source-code\">");

            var startLine = Math.Max(1, errorInfo.SourceContext.StartLine);
            var endLine = errorInfo.SourceContext.EndLine;
            var lines = errorInfo.SourceContext.Lines;

            for (var i = 0; i < lines.Length && (startLine + i) <= endLine; i++)
            {
                var lineNum = startLine + i;
                var isTarget = lineNum == errorInfo.Line;
                sb.AppendLine(
                    $"          <span class=\"voa-source-line {(isTarget ? "voa-source-line-error" : "")}\"><span class=\"voa-source-num\">{lineNum,4}</span> {EscapeHtml(lines[i])}</span>");
            }

            sb.AppendLine("        </pre>");
            sb.AppendLine("      </div>");
        }

        if (errorInfo.RelatedErrors is { Count: > 0 })
        {
            sb.AppendLine("      <div class=\"voa-error-related\">");
            sb.AppendLine("        <div class=\"voa-related-header\">相关错误</div>");

            foreach (var related in errorInfo.RelatedErrors)
            {
                sb.AppendLine("        <div class=\"voa-related-item\">");
                sb.AppendLine(
                    $"          <span class=\"voa-related-badge voa-error-badge-{related.Severity}\">{related.SeverityLabel}</span>");
                if (!string.IsNullOrEmpty(related.FilePath))
                {
                    sb.AppendLine($"          <span class=\"voa-related-file\">{EscapeHtml(related.FilePath)}</span>");
                }

                sb.AppendLine($"          <span class=\"voa-related-msg\">{EscapeHtml(related.Message)}</span>");
                sb.AppendLine("        </div>");
            }

            sb.AppendLine("      </div>");
        }

        if (errorInfo.Suggestions is { Count: > 0 })
        {
            sb.AppendLine("      <div class=\"voa-error-suggestions\">");
            sb.AppendLine("        <div class=\"voa-suggestions-header\">💡 修复建议</div>");
            sb.AppendLine("        <ul class=\"voa-suggestions-list\">");
            foreach (var suggestion in errorInfo.Suggestions)
            {
                sb.AppendLine($"          <li>{EscapeHtml(suggestion)}</li>");
            }
            sb.AppendLine("        </ul>");
            sb.AppendLine("      </div>");
        }

        if (!string.IsNullOrEmpty(errorInfo.DocumentationUrl))
        {
            sb.AppendLine($"      <div class=\"voa-error-docs\">");
            sb.AppendLine($"        <a class=\"voa-docs-link\" href=\"{EscapeHtml(errorInfo.DocumentationUrl)}\" target=\"_blank\" rel=\"noopener\">📖 查看文档</a>");
            sb.AppendLine("      </div>");
        }

        if (errorInfo.StackFrames is { Count: > 0 })
        {
            sb.AppendLine("      <div class=\"voa-error-stack\">");
            sb.AppendLine("        <div class=\"voa-stack-header\">堆栈跟踪</div>");
            sb.AppendLine("        <div class=\"voa-stack-frames\">");
            foreach (var frame in errorInfo.StackFrames)
            {
                var frameClass = frame.IsNative ? "voa-stack-native" : "voa-stack-frame";
                sb.AppendLine($"          <div class=\"{frameClass}\">");
                if (!string.IsNullOrEmpty(frame.FilePath))
                {
                    sb.AppendLine($"            <span class=\"voa-stack-file\">{EscapeHtml(frame.FilePath)}</span>");
                    if (frame.Line > 0)
                    {
                        sb.AppendLine($"            <span class=\"voa-stack-location\">:{frame.Line}{(frame.Column > 0 ? $":{frame.Column}" : "")}</span>");
                    }
                }
                sb.AppendLine($"            <span class=\"voa-stack-fn\">{EscapeHtml(frame.FunctionName)}</span>");
                sb.AppendLine("          </div>");
            }
            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    ///     向 HTML 页面注入 HMR 客户端脚本
    /// </summary>
    public string InjectHmrScript(string html, string host, int port)
    {
        var hmrScript = GenerateHmrClientScript(host, port);

        var bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        if (bodyCloseIndex < 0)
        {
            return html + hmrScript;
        }

        return html[..bodyCloseIndex] + hmrScript + html[bodyCloseIndex..];
    }

    /// <summary>
    ///     生成 HMR 客户端脚本（增强版：错误分类、可关闭覆盖层、增量更新、状态保持）
    /// </summary>
    private static string GenerateHmrClientScript(string host, int port)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<script>");
        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();
        sb.AppendLine($"  var wsUrl = 'ws://{host}:{port}';");
        sb.AppendLine("  var ws = null;");
        sb.AppendLine("  var reconnectTimer = null;");
        sb.AppendLine("  var reconnectAttempts = 0;");
        sb.AppendLine("  var MAX_RECONNECT = 10;");
        sb.AppendLine("  var overlayVisible = false;");
        sb.AppendLine("  var lastErrorTime = 0;");
        sb.AppendLine();
        sb.AppendLine("  function connect() {");
        sb.AppendLine("    try {");
        sb.AppendLine("      ws = new WebSocket(wsUrl);");
        sb.AppendLine("      ws.onopen = function() {");
        sb.AppendLine("        console.log('[VOA HMR] 已连接');");
        sb.AppendLine("        reconnectAttempts = 0;");
        sb.AppendLine("        removeOverlay();");
        sb.AppendLine("      };");
        sb.AppendLine("      ws.onmessage = function(event) {");
        sb.AppendLine("        try {");
        sb.AppendLine("          var msg = JSON.parse(event.data);");
        sb.AppendLine("          handleMessage(msg);");
        sb.AppendLine("        } catch(e) {");
        sb.AppendLine("          console.error('[VOA HMR] 消息解析失败', e);");
        sb.AppendLine("        }");
        sb.AppendLine("      };");
        sb.AppendLine("      ws.onclose = function() {");
        sb.AppendLine("        console.log('[VOA HMR] 连接断开');");
        sb.AppendLine("        scheduleReconnect();");
        sb.AppendLine("      };");
        sb.AppendLine("      ws.onerror = function() {");
        sb.AppendLine("        console.error('[VOA HMR] 连接错误');");
        sb.AppendLine("      };");
        sb.AppendLine("    } catch(e) {");
        sb.AppendLine("      scheduleReconnect();");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function scheduleReconnect() {");
        sb.AppendLine("    if (reconnectAttempts >= MAX_RECONNECT) {");
        sb.AppendLine("      console.error('[VOA HMR] 已达最大重连次数');");
        sb.AppendLine("      return;");
        sb.AppendLine("    }");
        sb.AppendLine("    reconnectAttempts++;");
        sb.AppendLine("    var delay = Math.min(1000 * Math.pow(2, reconnectAttempts), 30000);");
        sb.AppendLine("    console.log('[VOA HMR] ' + delay + 'ms 后重连（第 ' + reconnectAttempts + ' 次）');");
        sb.AppendLine("    reconnectTimer = setTimeout(connect, delay);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function handleMessage(msg) {");
        sb.AppendLine("    switch(msg.type) {");
        sb.AppendLine("      case 'update': handleUpdate(msg.payload); break;");
        sb.AppendLine("      case 'error': handleError(msg.payload); break;");
        sb.AppendLine("      case 'compile_error': handleCompileError(msg.payload); break;");
        sb.AppendLine("      case 'pong': break;");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function handleUpdate(payload) {");
        sb.AppendLine("    console.log('[VOA HMR] 文件变更：' + payload.path + '（' + payload.updateType + '）');");
        sb.AppendLine("    removeOverlay();");
        sb.AppendLine("    if (payload.updateType === 'style') {");
        sb.AppendLine("      reloadStylesheets();");
        sb.AppendLine("    } else if (payload.updateType === 'component') {");
        sb.AppendLine("      reloadComponent(payload.path);");
        sb.AppendLine("    } else {");
        sb.AppendLine("      location.reload();");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function handleError(payload) {");
        sb.AppendLine("    console.error('[VOA HMR] 运行时错误：' + payload.error);");
        sb.AppendLine("    showOverlay({");
        sb.AppendLine("      severity: 'runtime',");
        sb.AppendLine("      title: '运行时错误',");
        sb.AppendLine("      message: payload.error,");
        sb.AppendLine("      filePath: payload.filePath || null,");
        sb.AppendLine("      line: payload.line || 0,");
        sb.AppendLine("      column: payload.column || 0");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function handleCompileError(payload) {");
        sb.AppendLine("    console.error('[VOA HMR] 编译错误：' + payload.error);");
        sb.AppendLine("    showOverlay({");
        sb.AppendLine("      severity: payload.severity || 'error',");
        sb.AppendLine("      title: payload.title || '编译错误',");
        sb.AppendLine("      message: payload.error,");
        sb.AppendLine("      filePath: payload.filePath || null,");
        sb.AppendLine("      line: payload.line || 0,");
        sb.AppendLine("      column: payload.column || 0,");
        sb.AppendLine("      sourceLines: payload.sourceLines || null,");
        sb.AppendLine("      startLine: payload.startLine || 0,");
        sb.AppendLine("      relatedErrors: payload.relatedErrors || []");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function reloadStylesheets() {");
        sb.AppendLine("    var links = document.querySelectorAll('link[rel=stylesheet]');");
        sb.AppendLine("    for (var i = 0; i < links.length; i++) {");
        sb.AppendLine("      var href = links[i].href;");
        sb.AppendLine("      links[i].href = '';");
        sb.AppendLine("      links[i].href = href + '?t=' + Date.now();");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function reloadComponent(path) {");
        sb.AppendLine("    console.log('[VOA HMR] 组件热更新：' + path);");
        sb.AppendLine();
        sb.AppendLine("    var componentName = extractComponentName(path);");
        sb.AppendLine("    if (!componentName) {");
        sb.AppendLine("      console.warn('[VOA HMR] 无法从路径提取组件名，执行全量刷新');");
        sb.AppendLine("      location.reload();");
        sb.AppendLine("      return;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    var containers = document.querySelectorAll('[data-component=\\\"' + componentName + '\\\"]');");
        sb.AppendLine("    if (containers.length === 0) {");
        sb.AppendLine("      console.warn('[VOA HMR] 未找到组件容器：' + componentName + '，执行全量刷新');");
        sb.AppendLine("      location.reload();");
        sb.AppendLine("      return;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    var islandContainer = document.querySelector('[data-island][data-component=\\\"' + componentName + '\\\"]');");
        sb.AppendLine("    if (islandContainer && Voa.getIslandConfig) {");
        sb.AppendLine("      var config = Voa.getIslandConfig(componentName);");
        sb.AppendLine("      if (config) {");
        sb.AppendLine("        console.log('[VOA HMR] Island 组件重新水合：' + componentName);");
        sb.AppendLine("        Voa.mountIsland(componentName, islandContainer);");
        sb.AppendLine("        return;");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    for (var i = 0; i < containers.length; i++) {");
        sb.AppendLine("      var container = containers[i];");
        sb.AppendLine("      var factoryName = 'Voa_' + componentName;");
        sb.AppendLine("      if (window[factoryName] && typeof window[factoryName] === 'function') {");
        sb.AppendLine("        var newEl = window[factoryName]();");
        sb.AppendLine("        container.innerHTML = '';");
        sb.AppendLine("        if (newEl) {");
        sb.AppendLine("          container.parentNode.replaceChild(newEl, container);");
        sb.AppendLine("        }");
        sb.AppendLine("        console.log('[VOA HMR] 组件已更新：' + componentName);");
        sb.AppendLine("        return;");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    console.warn('[VOA HMR] 未找到组件工厂函数，执行全量刷新');");
        sb.AppendLine("    location.reload();");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function extractComponentName(path) {");
        sb.AppendLine("    var parts = path.replace(/\\\\\\\\/g, '/').split('/');");
        sb.AppendLine("    var filename = parts[parts.length - 1];");
        sb.AppendLine("    if (filename.endsWith('.awsl')) {");
        sb.AppendLine("      filename = filename.slice(0, -5);");
        sb.AppendLine("    } else if (filename.endsWith('.v')) {");
        sb.AppendLine("      filename = filename.slice(0, -2);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    return filename.charAt(0).toUpperCase() + filename.slice(1);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function showOverlay(info) {");
        sb.AppendLine("    lastErrorTime = Date.now();");
        sb.AppendLine("    removeOverlay();");
        sb.AppendLine("    overlayVisible = true;");
        sb.AppendLine();
        sb.AppendLine("    var overlay = document.createElement('div');");
        sb.AppendLine("    overlay.id = 'voa-error-overlay';");
        sb.AppendLine("    overlay.innerHTML = buildOverlayHtml(info);");
        sb.AppendLine("    document.body.appendChild(overlay);");
        sb.AppendLine();
        sb.AppendLine("    var closeBtn = overlay.querySelector('.voa-overlay-close');");
        sb.AppendLine("    if (closeBtn) {");
        sb.AppendLine("      closeBtn.addEventListener('click', function() { removeOverlay(); });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    document.addEventListener('keydown', handleEscKey);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function handleEscKey(e) {");
        sb.AppendLine("    if (e.key === 'Escape' && overlayVisible) {");
        sb.AppendLine("      removeOverlay();");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function removeOverlay() {");
        sb.AppendLine("    overlayVisible = false;");
        sb.AppendLine("    var existing = document.getElementById('voa-error-overlay');");
        sb.AppendLine("    if (existing) existing.remove();");
        sb.AppendLine("    document.removeEventListener('keydown', handleEscKey);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function buildOverlayHtml(info) {");
        sb.AppendLine("    var severityColors = {");
        sb.AppendLine("      'error': { bg: '#ff6b6b', label: '错误' },");
        sb.AppendLine("      'warning': { bg: '#ffd93d', label: '警告' },");
        sb.AppendLine("      'syntax': { bg: '#ff9f43', label: '语法错误' },");
        sb.AppendLine("      'compile': { bg: '#ee5a24', label: '编译错误' },");
        sb.AppendLine("      'runtime': { bg: '#ff6b6b', label: '运行时错误' }");
        sb.AppendLine("    };");
        sb.AppendLine("    var sev = severityColors[info.severity] || severityColors['error'];");
        sb.AppendLine();
        sb.AppendLine("    var html = '<div class=\"voa-overlay-container\">';");
        sb.AppendLine("    html += '<div class=\"voa-overlay-header\">';");
        sb.AppendLine("    html += '<div class=\"voa-overlay-title-row\">';");
        sb.AppendLine("    html += '<span class=\"voa-overlay-badge\" style=\"background:' + sev.bg + '\">' + sev.label + '</span>';");
        sb.AppendLine("    html += '<span class=\"voa-overlay-title\">' + escapeHtml(info.title) + '</span>';");
        sb.AppendLine("    html += '</div>';");
        sb.AppendLine("    html += '<button class=\"voa-overlay-close\" title=\"关闭 (Esc)\">✕</button>';");
        sb.AppendLine("    html += '</div>';");
        sb.AppendLine();
        sb.AppendLine("    if (info.filePath) {");
        sb.AppendLine("      html += '<div class=\"voa-overlay-location\">';");
        sb.AppendLine("      html += '<span class=\"voa-overlay-file\">📄 ' + escapeHtml(info.filePath) + '</span>';");
        sb.AppendLine("      if (info.line > 0) {");
        sb.AppendLine("        html += '<span class=\"voa-overlay-line\">行 ' + info.line + (info.column > 0 ? ', 列 ' + info.column : '') + '</span>';");
        sb.AppendLine("      }");
        sb.AppendLine("      html += '</div>';");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    html += '<pre class=\"voa-overlay-message\">' + escapeHtml(info.message) + '</pre>';");
        sb.AppendLine();
        sb.AppendLine("    if (info.sourceLines && info.sourceLines.length > 0) {");
        sb.AppendLine("      html += '<div class=\"voa-overlay-source\">';");
        sb.AppendLine("      html += '<div class=\"voa-source-label\">源码上下文</div>';");
        sb.AppendLine("      html += '<pre class=\"voa-source-code\">';");
        sb.AppendLine("      for (var i = 0; i < info.sourceLines.length; i++) {");
        sb.AppendLine("        var lineNum = info.startLine + i;");
        sb.AppendLine("        var isTarget = lineNum === info.line;");
        sb.AppendLine("        html += '<div class=\"voa-source-line' + (isTarget ? ' voa-source-error' : '') + '\">';");
        sb.AppendLine("        html += '<span class=\"voa-source-num\">' + lineNum + '</span>';");
        sb.AppendLine("        html += '<span class=\"voa-source-text\">' + escapeHtml(info.sourceLines[i]) + '</span>';");
        sb.AppendLine("        html += '</div>';");
        sb.AppendLine("      }");
        sb.AppendLine("      html += '</pre></div>';");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    if (info.relatedErrors && info.relatedErrors.length > 0) {");
        sb.AppendLine("      html += '<div class=\"voa-overlay-related\">';");
        sb.AppendLine("      html += '<div class=\"voa-related-label\">相关错误（' + info.relatedErrors.length + '）</div>';");
        sb.AppendLine("      for (var j = 0; j < info.relatedErrors.length; j++) {");
        sb.AppendLine("        var rel = info.relatedErrors[j];");
        sb.AppendLine("        html += '<div class=\"voa-related-item\">';");
        sb.AppendLine("        html += '<span class=\"voa-related-file\">' + escapeHtml(rel.filePath || '') + '</span>';");
        sb.AppendLine("        html += '<span class=\"voa-related-msg\">' + escapeHtml(rel.message || '') + '</span>';");
        sb.AppendLine("        html += '</div>';");
        sb.AppendLine("      }");
        sb.AppendLine("      html += '</div>';");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    html += '</div>';");
        sb.AppendLine("    return html;");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function escapeHtml(str) {");
        sb.AppendLine("    if (!str) return '';");
        sb.AppendLine("    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\"/g,'&quot;');");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  connect();");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");

        return sb.ToString();
    }

    private static string GetErrorStyles()
    {
        var sb = new StringBuilder();
        sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("body { background: #1a1a2e; color: #e0e0e0; font-family: 'Segoe UI', Consolas, monospace; }");
        sb.AppendLine(".voa-error-overlay { max-width: 900px; margin: 60px auto; padding: 0 24px; }");
        sb.AppendLine(".voa-error-header { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; }");
        sb.AppendLine(".voa-error-code { background: #ff6b6b; color: #fff; padding: 4px 12px; border-radius: 4px; font-size: 18px; font-weight: bold; }");
        sb.AppendLine(".voa-error-title { font-size: 20px; color: #ffd93d; }");
        sb.AppendLine(".voa-error-file { color: #4ecdc4; font-size: 14px; margin-bottom: 16px; padding: 8px 12px; background: #16213e; border-radius: 4px; }");
        sb.AppendLine(".voa-error-message { background: #16213e; padding: 20px; border-radius: 8px; font-size: 14px; line-height: 1.6; overflow-x: auto; white-space: pre-wrap; border-left: 4px solid #ff6b6b; }");
        return sb.ToString();
    }

    private static string GetStructuredErrorStyles()
    {
        var sb = new StringBuilder();
        sb.AppendLine(".voa-error-badge { padding: 3px 10px; border-radius: 4px; font-size: 12px; font-weight: 600; color: #fff; }");
        sb.AppendLine(".voa-error-badge-error { background: #ff6b6b; }");
        sb.AppendLine(".voa-error-badge-warning { background: #ffd93d; color: #333; }");
        sb.AppendLine(".voa-error-badge-syntax { background: #ff9f43; }");
        sb.AppendLine(".voa-error-badge-compile { background: #ee5a24; }");
        sb.AppendLine(".voa-error-location { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; padding: 8px 12px; background: #16213e; border-radius: 4px; }");
        sb.AppendLine(".voa-error-line { color: #8b949e; font-size: 13px; }");
        sb.AppendLine(".voa-error-source { margin-top: 16px; }");
        sb.AppendLine(".voa-source-header { font-size: 12px; color: #8b949e; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }");
        sb.AppendLine(".voa-source-code { background: #0d1117; padding: 12px; border-radius: 6px; overflow-x: auto; font-size: 13px; line-height: 1.6; }");
        sb.AppendLine(".voa-source-line { display: flex; gap: 12px; }");
        sb.AppendLine(".voa-source-line-error { background: rgba(248, 81, 73, 0.15); border-left: 3px solid #ff6b6b; margin: 0 -12px; padding: 0 12px; }");
        sb.AppendLine(".voa-source-num { color: #6e7681; min-width: 36px; text-align: right; user-select: none; }");
        sb.AppendLine(".voa-error-related { margin-top: 16px; }");
        sb.AppendLine(".voa-related-header { font-size: 12px; color: #8b949e; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }");
        sb.AppendLine(".voa-related-item { display: flex; align-items: center; gap: 8px; padding: 6px 12px; border-radius: 4px; margin-bottom: 4px; background: #16213e; }");
        sb.AppendLine(".voa-related-file { color: #4ecdc4; font-size: 13px; font-family: monospace; }");
        sb.AppendLine(".voa-related-msg { color: #e0e0e0; font-size: 13px; flex: 1; }");
        sb.AppendLine(".voa-error-suggestions { margin-top: 16px; }");
        sb.AppendLine(".voa-suggestions-header { font-size: 12px; color: #ffd93d; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }");
        sb.AppendLine(".voa-suggestions-list { list-style: none; padding: 0; }");
        sb.AppendLine(".voa-suggestions-list li { padding: 6px 12px; background: rgba(255, 217, 61, 0.08); border-left: 3px solid #ffd93d; margin-bottom: 4px; border-radius: 0 4px 4px 0; font-size: 13px; color: #e0e0e0; }");
        sb.AppendLine(".voa-error-docs { margin-top: 12px; }");
        sb.AppendLine(".voa-docs-link { display: inline-flex; align-items: center; gap: 6px; padding: 6px 14px; background: rgba(88, 166, 255, 0.1); border: 1px solid rgba(88, 166, 255, 0.3); border-radius: 6px; color: #58a6ff; font-size: 13px; text-decoration: none; transition: all 0.2s; }");
        sb.AppendLine(".voa-docs-link:hover { background: rgba(88, 166, 255, 0.2); border-color: #58a6ff; }");
        sb.AppendLine(".voa-error-stack { margin-top: 16px; }");
        sb.AppendLine(".voa-stack-header { font-size: 12px; color: #8b949e; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }");
        sb.AppendLine(".voa-stack-frames { background: #0d1117; border-radius: 6px; overflow: hidden; }");
        sb.AppendLine(".voa-stack-frame { display: flex; align-items: center; gap: 12px; padding: 6px 12px; border-bottom: 1px solid #21262d; font-size: 12px; }");
        sb.AppendLine(".voa-stack-frame:last-child { border-bottom: none; }");
        sb.AppendLine(".voa-stack-native { display: flex; align-items: center; gap: 12px; padding: 6px 12px; background: rgba(110, 118, 129, 0.1); border-bottom: 1px solid #21262d; font-size: 12px; color: #6e7681; font-style: italic; }");
        sb.AppendLine(".voa-stack-file { color: #4ecdc4; font-family: monospace; }");
        sb.AppendLine(".voa-stack-location { color: #8b949e; font-family: monospace; }");
        sb.AppendLine(".voa-stack-fn { color: #e0e0e0; font-family: monospace; }");
        return sb.ToString();
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}