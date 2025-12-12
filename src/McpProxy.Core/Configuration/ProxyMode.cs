namespace McpProxy.Core.Configuration;

/// <summary>
/// 定义代理的运行模式
/// </summary>
public enum ProxyMode
{
    /// <summary>
    /// 连接到远程SSE服务器并通过Stdio方式暴露
    /// </summary>
    SseToStdio,

    /// <summary>
    /// 连接到本地Stdio服务器（单个或多个）并通过HTTP/SSE方式暴露
    /// </summary>
    StdioToHttp
}
