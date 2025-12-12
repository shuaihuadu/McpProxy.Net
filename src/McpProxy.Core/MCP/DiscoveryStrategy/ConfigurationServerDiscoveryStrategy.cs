namespace McpProxy;

/// <summary>
/// 从配置系统中发现MCP服务器
/// 此策略从IOptions&lt;StdioMcpServersOptions&gt;配置中加载服务器信息
/// </summary>
/// <param name="options">配置服务行为的选项</param>
/// <param name="logger">此发现策略的日志记录器实例</param>
public sealed class ConfigurationServerDiscoveryStrategy(IOptions<StdioMcpServersOptions> options, ILogger<ConfigurationServerDiscoveryStrategy> logger) : BaseDiscoveryStrategy(logger)
{
    /// <summary>
    /// 获取stdio MCP服务器配置选项
    /// </summary>
    private readonly IOptions<StdioMcpServersOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc/>
    public override Task<IEnumerable<IMcpServerProvider>> DiscoverServersAsync(CancellationToken cancellationToken)
    {
        // 验证服务器配置不为null
        if (this._options.Value.Servers is null)
        {
            this._logger.LogWarning("No servers configured in the options.");
            return Task.FromResult(Enumerable.Empty<IMcpServerProvider>());
        }

        // 从配置中创建服务器提供者集合，只包含启用的服务器
        // Enabled为null或true时表示启用，为false时表示禁用
        IEnumerable<IMcpServerProvider> servers = this._options.Value.Servers
            .Where(server => server.Value.Enabled != false) // 只过滤掉明确禁用的服务器（Enabled为false）
            .Select(server => new StdioMcpServerProvider(server.Key, server.Value))
            .Cast<IMcpServerProvider>();

        int serverCount = servers.Count();

        this._logger.LogInformation("Discovered {ServerCount} enabled MCP servers from configuration.", serverCount);

        return Task.FromResult(servers);
    }
}
