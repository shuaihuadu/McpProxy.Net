namespace McpProxy;

/// <summary>
/// 提供基于命名MCP服务器配置的MCP服务器实现，只支持stdio传输机制
/// </summary>
/// <param name="id">MCP服务器的唯一标识</param>
/// <param name="serverInfo">MCP服务器配置信息</param>
public class NamedMcpServerProvider(string id, NamedMcpServerInfo serverInfo) : IMcpServerProvider
{
    private readonly string _id = id;
    private readonly NamedMcpServerInfo _serverInfo = serverInfo;

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
        //bool isStdioTransport = string.Equals(this._serverInfo.Type, TransportTypes.StdIo, StringComparison.OrdinalIgnoreCase)
        //    || !string.IsNullOrWhiteSpace(this._serverInfo.Command);

        if (string.IsNullOrWhiteSpace(_serverInfo.Command))
        {
            throw new InvalidOperationException($"Named server '{_id}' does not have a valid command for stdio transport.");
        }

        Dictionary<string, string?> environmentVariables = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string?)e.Value);

        if (_serverInfo.Env != null)
        {
            foreach (var kvp in _serverInfo.Env)
            {
                environmentVariables[kvp.Key] = kvp.Value;
            }
        }

        StdioClientTransportOptions transportOptions = new()
        {
            Name = _id,
            Command = _serverInfo.Command,
            Arguments = _serverInfo.Args,
            EnvironmentVariables = environmentVariables,
            WorkingDirectory = _serverInfo.Cwd
            // TODO StandardErrorLines
        };

        StdioClientTransport clientTransport = new(transportOptions);

        return await McpClient.CreateAsync(clientTransport, clientOptions, cancellationToken: cancellationToken);
    }
}