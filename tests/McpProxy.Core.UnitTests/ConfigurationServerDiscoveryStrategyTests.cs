namespace McpProxy.Core.UnitTests;

/// <summary>
/// ConfigurationServerDiscoveryStrategy类的单元测试
/// </summary>
[TestClass]
public sealed class ConfigurationServerDiscoveryStrategyTests
{
    /// <summary>
    /// 获取或设置测试上下文
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// 创建用于测试的配置选项
    /// </summary>
    private static Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> CreateOptions(StdioMcpServersOptions options)
    {
        return Microsoft.Extensions.Options.Options.Create(options);
    }

    /// <summary>
    /// 创建用于测试的日志记录器
    /// </summary>
    private static Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> CreateLogger()
    {
        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            // 简单的日志配置
        });
        return loggerFactory.CreateLogger(typeof(ConfigurationServerDiscoveryStrategy).FullName ?? "Test") as Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> ?? null!;
    }

    /// <summary>
    /// 测试DiscoverServersAsync在没有配置服务器时返回空集合
    /// </summary>
    [TestMethod]
    public async Task DiscoverServersAsync_WithNoServers_ReturnsEmptyCollection()
    {
        // Arrange
        StdioMcpServersOptions options = new()
        {
            Servers = null
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        IEnumerable<IMcpServerProvider> result = await strategy.DiscoverServersAsync(this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    /// <summary>
    /// 测试DiscoverServersAsync正确返回配置的服务器
    /// </summary>
    [TestMethod]
    public async Task DiscoverServersAsync_WithConfiguredServers_ReturnsCorrectProviders()
    {
        // Arrange
        Dictionary<string, StdioMcpServer> servers = new()
        {
            {
                "server1",
                new StdioMcpServer
                {
                    Name = "Server 1",
                    Command = "test-command",
                    Description = "Test Server 1"
                }
            },
            {
                "server2",
                new StdioMcpServer
                {
                    Name = "Server 2",
                    Command = "test-command2",
                    Description = "Test Server 2"
                }
            }
        };

        StdioMcpServersOptions options = new()
        {
            Servers = servers
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        IEnumerable<IMcpServerProvider> result = await strategy.DiscoverServersAsync(this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
    }

    /// <summary>
    /// 测试DiscoverServersAsync过滤掉已禁用的服务器
    /// </summary>
    [TestMethod]
    public async Task DiscoverServersAsync_WithDisabledServers_FiltersDisabledServers()
    {
        // Arrange
        Dictionary<string, StdioMcpServer> servers = new()
        {
            {
                "server1",
                new StdioMcpServer
                {
                    Name = "Server 1",
                    Command = "test-command",
                    Description = "Test Server 1",
                    Enabled = false // 明确禁用
                }
            },
            {
                "server2",
                new StdioMcpServer
                {
                    Name = "Server 2",
                    Command = "test-command2",
                    Description = "Test Server 2",
                    Enabled = true // 明确启用
                }
            },
            {
                "server3",
                new StdioMcpServer
                {
                    Name = "Server 3",
                    Command = "test-command3",
                    Description = "Test Server 3"
                    // Enabled为null，默认启用
                }
            }
        };

        StdioMcpServersOptions options = new()
        {
            Servers = servers
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        IEnumerable<IMcpServerProvider> result = await strategy.DiscoverServersAsync(this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count()); // 只有server2和server3，server1被过滤掉
    }

    /// <summary>
    /// 测试DiscoverServersAsync包含所有启用的服务器（Enabled为true或null）
    /// </summary>
    [TestMethod]
    public async Task DiscoverServersAsync_WithEnabledAndNullEnabledServers_IncludesAll()
    {
        // Arrange
        Dictionary<string, StdioMcpServer> servers = new()
        {
            {
                "server1",
                new StdioMcpServer
                {
                    Name = "Server 1",
                    Command = "test-command",
                    Description = "Test Server 1",
                    Enabled = true // 明确启用
                }
            },
            {
                "server2",
                new StdioMcpServer
                {
                    Name = "Server 2",
                    Command = "test-command2",
                    Description = "Test Server 2"
                    // Enabled为null，默认启用
                }
            }
        };

        StdioMcpServersOptions options = new()
        {
            Servers = servers
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        IEnumerable<IMcpServerProvider> result = await strategy.DiscoverServersAsync(this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count()); // 两个服务器都应该被包含
    }

    /// <summary>
    /// 测试DiscoverServersAsync返回的提供者可以创建元数据
    /// </summary>
    [TestMethod]
    public async Task DiscoverServersAsync_ReturnsProvidersWithCorrectMetadata()
    {
        // Arrange
        string serverId = "testServer";
        string serverName = "Test Server";
        string serverDescription = "Test Description";

        Dictionary<string, StdioMcpServer> servers = new()
        {
            {
                serverId,
                new StdioMcpServer
                {
                    Name = serverName,
                    Command = "test-command",
                    Description = serverDescription
                }
            }
        };

        StdioMcpServersOptions options = new()
        {
            Servers = servers
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        IEnumerable<IMcpServerProvider> result = await strategy.DiscoverServersAsync(this.TestContext.CancellationToken);
        IMcpServerProvider provider = result.First();
        McpServerMetadata metadata = provider.CreateMetadata();

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(serverId, metadata.Id);
        Assert.AreEqual(serverName, metadata.Name);
        Assert.AreEqual(serverDescription, metadata.Description);
    }

    /// <summary>
    /// 测试DisposeAsync正确释放资源
    /// </summary>
    [TestMethod]
    public async Task DisposeAsync_DisposesResourcesCorrectly()
    {
        // Arrange
        StdioMcpServersOptions options = new()
        {
            Servers = new Dictionary<string, StdioMcpServer>()
        };

        Microsoft.Extensions.Options.IOptions<StdioMcpServersOptions> optionsWrapper = CreateOptions(options);
        Microsoft.Extensions.Logging.ILogger<ConfigurationServerDiscoveryStrategy> logger = CreateLogger();
        ConfigurationServerDiscoveryStrategy strategy = new(optionsWrapper, logger);

        // Act
        await strategy.DisposeAsync();

        // Assert - 不应抛出异常
        Assert.IsTrue(true);
    }
}
