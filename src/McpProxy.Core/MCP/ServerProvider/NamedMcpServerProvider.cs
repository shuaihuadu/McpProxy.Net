namespace McpProxy;

/// <summary>
/// 提供基于命名MCP服务器配置的MCP服务器实现，支持stdio传输机制
/// </summary>
/// <param name="id">MCP服务器的唯一标识符</param>
/// <param name="serverInfo">MCP服务器配置信息</param>
public class NamedMcpServerProvider(string id, NamedMcpServerInfo serverInfo) : IMcpServerProvider
{
    /// <summary>
    /// 获取MCP服务器的唯一标识符
    /// </summary>
    private readonly string _id = id ?? throw new ArgumentNullException(nameof(id));

    /// <summary>
    /// 获取MCP服务器配置信息
    /// </summary>
    private readonly NamedMcpServerInfo _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));

    /// <inheritdoc/>
    public McpServerMetadata CreateMetadata()
    {
        return new()
        {
            Id = this._id,
            Name = this._serverInfo.Name ?? this._id,
            Title = this._serverInfo.Title,
            Description = this._serverInfo.Description ?? string.Empty
        };
    }

    /// <inheritdoc/>
    public async Task<McpClient> CreateClientAsync(McpClientOptions clientOptions, CancellationToken cancellationToken = default)
    {
        // 验证命令是否存在（stdio传输必需）
        if (string.IsNullOrWhiteSpace(this._serverInfo.Command))
        {
            throw new InvalidOperationException($"Named server '{this._id}' does not have a valid command for stdio transport.");
        }

        // 收集环境变量：首先从系统环境变量获取
        Dictionary<string, string?> environmentVariables = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

        // 合并服务器配置中的环境变量（可覆盖系统环境变量）
        if (this._serverInfo.Env != null)
        {
            foreach (KeyValuePair<string, string?> kvp in this._serverInfo.Env)
            {
                environmentVariables[kvp.Key] = kvp.Value;
            }
        }

        // 创建stdio传输选项
        StdioClientTransportOptions transportOptions = new()
        {
            Name = this._id,
            Command = this._serverInfo.Command,
            Arguments = this._serverInfo.Args,
            EnvironmentVariables = environmentVariables,
            WorkingDirectory = this._serverInfo.Cwd
            // TODO: 考虑添加StandardErrorLines处理
        };

        // 创建stdio客户端传输
        StdioClientTransport clientTransport = new(transportOptions);

        // 创建并返回MCP客户端
        return await McpClient.CreateAsync(clientTransport, clientOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}