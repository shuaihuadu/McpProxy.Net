namespace McpProxy;

/// <summary>
/// 定义MCP服务器提供者的接口，用于创建服务器元数据和客户端
/// </summary>
public interface IMcpServerProvider
{
    /// <summary>
    /// 创建描述此服务器提供者的元数据
    /// </summary>
    /// <returns>包含服务器标识和描述的元数据对象</returns>
    McpServerMetadata CreateMetadata();

    /// <summary>
    /// 创建可与此服务器通信的MCP客户端
    /// </summary>
    /// <param name="clientOptions">用于配置客户端行为的选项</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>配置完成且可供使用的MCP客户端</returns>
    /// <exception cref="ArgumentException">当服务器配置未指定有效的传输类型（缺少URL或stdio配置）时抛出</exception>
    /// <exception cref="InvalidOperationException">当服务器配置有效但客户端创建失败时抛出（例如，stdio传输缺少命令、依赖项问题或外部进程失败）</exception>
    Task<McpClient> CreateClientAsync(McpClientOptions clientOptions, CancellationToken cancellationToken = default);
}
