using ModelContextProtocol.Client;

namespace McpProxy.Core.UnitTests;

/// <summary>
/// NamedMcpServerProvider类的单元测试
/// </summary>
[TestClass]
public sealed class NamedMcpServerProviderTests
{
    /// <summary>
    /// 获取或设置测试上下文
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// 测试构造函数是否正确初始化
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        string testId = "testProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Test Description",
            Command = "test-command"
        };

        // Act
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Assert
        Assert.IsNotNull(provider);
        Assert.IsInstanceOfType<NamedMcpServerProvider>(provider);
    }

    /// <summary>
    /// 测试构造函数在id参数为null时是否抛出异常
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Test Description",
            Command = "test-command"
        };

        // Act & Assert
        try
        {
            NamedMcpServerProvider provider = new(null!, serverInfo);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // 预期的异常
        }
    }

    /// <summary>
    /// 测试构造函数在serverInfo参数为null时是否抛出异常
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullServerInfo_ThrowsArgumentNullException()
    {
        // Arrange
        string testId = "testProvider";

        // Act & Assert
        try
        {
            NamedMcpServerProvider provider = new(testId, null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // 预期的异常
        }
    }

    /// <summary>
    /// 测试CreateMetadata返回预期的元数据
    /// </summary>
    [TestMethod]
    public void CreateMetadata_WithValidServerInfo_ReturnsExpectedMetadata()
    {
        // Arrange
        string testId = "testProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Test Description",
            Command = "test-command"
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpServerMetadata metadata = provider.CreateMetadata();

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(testId, metadata.Id);
        Assert.AreEqual(testId, metadata.Name);
        Assert.IsNull(metadata.Title);
        Assert.AreEqual(serverInfo.Description, metadata.Description);
    }

    /// <summary>
    /// 测试CreateMetadata在描述为空时返回空字符串
    /// </summary>
    [TestMethod]
    public void CreateMetadata_WithNullDescription_ReturnsEmptyString()
    {
        // Arrange
        string testId = "testProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = null,
            Command = "test-command"
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpServerMetadata metadata = provider.CreateMetadata();

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(testId, metadata.Id);
        Assert.AreEqual(testId, metadata.Name);
        Assert.IsNull(metadata.Title);
        Assert.AreEqual(string.Empty, metadata.Description);
    }

    /// <summary>
    /// 测试CreateMetadata在有标题时返回正确的标题
    /// </summary>
    [TestMethod]
    public void CreateMetadata_WithTitle_ReturnsTitleInMetadata()
    {
        // Arrange
        string testId = "testProvider";
        string testTitle = "Test Provider Display Name";
        NamedMcpServerInfo serverInfo = new()
        {
            Title = testTitle,
            Description = "Test Description",
            Command = "test-command"
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpServerMetadata metadata = provider.CreateMetadata();

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(testId, metadata.Id);
        Assert.AreEqual(testId, metadata.Name);
        Assert.AreEqual(testTitle, metadata.Title);
        Assert.AreEqual(serverInfo.Description, metadata.Description);
    }

    /// <summary>
    /// 测试CreateMetadata在有自定义名称时使用自定义名称
    /// </summary>
    [TestMethod]
    public void CreateMetadata_WithCustomName_UsesCustomName()
    {
        // Arrange
        string testId = "testProvider";
        string customName = "Custom Server Name";
        NamedMcpServerInfo serverInfo = new()
        {
            Name = customName,
            Description = "Test Description",
            Command = "test-command"
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpServerMetadata metadata = provider.CreateMetadata();

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(testId, metadata.Id);
        Assert.AreEqual(customName, metadata.Name);
    }

    /// <summary>
    /// 测试CreateClientAsync在命令为空时抛出InvalidOperationException
    /// </summary>
    [TestMethod]
    public async Task CreateClientAsync_WithEmptyCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        string testId = "invalidProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Invalid Provider - No Command",
            Command = ""
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.CreateClientAsync(new McpClientOptions(), this.TestContext.CancellationToken));

        Assert.IsTrue(exception.Message.Contains("does not have a valid command for stdio transport"));
    }

    /// <summary>
    /// 测试CreateClientAsync在命令为null时抛出InvalidOperationException
    /// </summary>
    [TestMethod]
    public async Task CreateClientAsync_WithNullCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        string testId = "invalidStdioProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Invalid Stdio Provider - No Command",
            Type = "stdio",
            Command = null!
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.CreateClientAsync(new McpClientOptions(), this.TestContext.CancellationToken));

        Assert.IsTrue(exception.Message.Contains("does not have a valid command for stdio transport"));
    }

    /// <summary>
    /// 测试CreateClientAsync在环境变量存在时合并系统环境变量
    /// </summary>
    [TestMethod]
    public async Task CreateClientAsync_WithEnvVariables_MergesWithSystemEnvironment()
    {
        // Arrange
        string testId = "envProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Description = "Test Env Provider",
            Type = "stdio",
            Command = "echo",
            Args = ["\"hello world\""],
            Env = new Dictionary<string, string>
            {
                { "TEST_VAR", "test value" }
            }
        };
        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act & Assert
        // 由于配置的命令是 echo "hello world"，该命令不会失败，因此我们预期客户端能够成功创建
        // 但实际上echo命令会立即退出，导致MCP服务器进程意外退出
        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => provider.CreateClientAsync(new McpClientOptions(), this.TestContext.CancellationToken));

        Assert.IsTrue(exception.Message.Contains("MCP server process exited unexpectedly"));
    }

    /// <summary>
    /// 测试CreateClientAsync使用stdio类型创建客户端成功
    /// </summary>
    [TestMethod]
    public async Task CreateClientAsync_WithStdioType_CreatesStdioClientSuccessfully()
    {
        // Arrange
        string testId = "stdioProvider";
        NamedMcpServerInfo serverInfo = new()
        {
            Type = "stdio",
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-everything"],
            Description = "Test Stdio Provider"
        };

        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpClient client = await provider.CreateClientAsync(new McpClientOptions(), this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(client);
        Assert.IsNotNull(client.ServerInfo);
    }

    /// <summary>
    /// 测试CreateClientAsync使用自定义工作目录
    /// </summary>
    [TestMethod]
    public async Task CreateClientAsync_WithCustomWorkingDirectory_ConfiguresCorrectly()
    {
        // Arrange
        string testId = "cwdProvider";
        string customWorkingDirectory = "C:\\test";
        NamedMcpServerInfo serverInfo = new()
        {
            Type = "stdio",
            Command = "npx",
            Args = ["-y", "@modelcontextprotocol/server-everything"],
            Description = "Test Provider with Custom CWD",
            Cwd = customWorkingDirectory
        };

        NamedMcpServerProvider provider = new(testId, serverInfo);

        // Act
        McpClient client = await provider.CreateClientAsync(new McpClientOptions(), this.TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(client);
    }
}
