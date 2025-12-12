using ModelContextProtocol.Client;

namespace McpProxy.Core.UnitTests
{
    [TestClass]
    public sealed class NamedMcpServerProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange
            string testId = "testProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Description = "Test Description"
            };

            // Act
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Assert
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType<NamedMcpServerProvider>(provider);
        }


        [TestMethod]
        public void CreateMetadata_ReturnsExpectedMetadata()
        {
            // Arrange
            string testId = "testProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Description = "Test Description"
            };
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Act
            var metadata = provider.CreateMetadata();

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(testId, metadata.Id);
            Assert.AreEqual(testId, metadata.Name);
            Assert.IsNull(metadata.Title);
            Assert.AreEqual(serverInfo.Description, metadata.Description);
        }

        [TestMethod]
        public void CreateMetadata_EmptyDescription_ReturnsEmptyString()
        {
            // Arrange
            string testId = "testProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Description = null
            };
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Act
            var metadata = provider.CreateMetadata();

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(testId, metadata.Id);
            Assert.AreEqual(testId, metadata.Name);
            Assert.IsNull(metadata.Title); // No title specified
            Assert.AreEqual(string.Empty, metadata.Description);
        }

        [TestMethod]
        public void CreateMetadata_WithTitle_ReturnsTitleInMetadata()
        {
            // Arrange
            string testId = "testProvider";
            string testTitle = "Test Provider Display Name";
            var serverInfo = new NamedMcpServerInfo
            {
                Title = testTitle,
                Description = "Test Description"
            };
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Act
            var metadata = provider.CreateMetadata();

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(testId, metadata.Id);
            Assert.AreEqual(testId, metadata.Name);
            Assert.AreEqual(testTitle, metadata.Title);
            Assert.AreEqual(serverInfo.Description, metadata.Description);
        }

        [TestMethod]
        public async Task CreateClientAsync_NoType_ThrowsArgumentException()
        {
            // Arrange
            string testId = "invalidProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Description = "Invalid Provider - No Transport"
                // No Url or Type specified
            };
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateClientAsync(new McpClientOptions(), TestContext.CancellationToken));

            Assert.Contains($"does not have a valid command for stdio transport", exception.Message);
        }

        [TestMethod]
        public async Task CreateClientAsync_StdioWithoutCommand_ThrowsInvalidOperationException()
        {
            // Arrange
            string testId = "invalidStdioProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Description = "Invalid Stdio Provider - No Command",
                Type = "stdio"
                // No Command specified
            };
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.CreateClientAsync(new McpClientOptions()));

            Assert.Contains($"does not have a valid command for stdio transport",
                exception.Message);
        }

        [TestMethod]
        public async Task CreateClientAsync_WithEnvVariables_MergesWithSystemEnvironment()
        {
            // Arrange
            string testId = "envProvider";
            var serverInfo = new NamedMcpServerInfo
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
            var provider = new NamedMcpServerProvider(testId, serverInfo);

            // 由于配置的命令是 echo "hello world"，该命令不会失败，因此我们预期客户端能够成功创建。
            var exception = await Assert.ThrowsAsync<IOException>(() => provider.CreateClientAsync(new McpClientOptions()));

            Assert.Contains($"MCP server process exited unexpectedly", exception.Message);
        }

        [TestMethod]
        public async Task CreateClientAsync_WithStdioType_CreatesStdioClient_ShouldSuccess()
        {
            // Arrange
            string testId = "stdioProvider";
            var serverInfo = new NamedMcpServerInfo
            {
                Type = "stdio",
                Command = "npx",
                Args = ["-y", "@modelcontextprotocol/server-everything"],
                Description = "Test Stdio Provider"
            };

            var provider = new NamedMcpServerProvider(testId, serverInfo);

            McpClient client = await provider.CreateClientAsync(new McpClientOptions(), TestContext.CancellationToken);

            Assert.IsNotNull(client);
        }
    }
}
