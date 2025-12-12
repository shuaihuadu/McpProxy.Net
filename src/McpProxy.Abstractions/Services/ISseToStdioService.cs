namespace McpProxy.Abstractions.Services;

/// <summary>
/// SSE到Stdio代理服务接口
/// 用于将远程SSE MCP服务器转换为本地Stdio接口
/// </summary>
public interface ISseToStdioService : IProxyService
{
    // 继承自 IProxyService.RunAsync()
}
