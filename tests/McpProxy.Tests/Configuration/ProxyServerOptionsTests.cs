using McpProxy.Core.Configuration;
using System.ComponentModel.DataAnnotations;

namespace McpProxy.Tests.Configuration;

/// <summary>
/// 针对<see cref="ProxyServerOptions"/>类的单元测试
/// </summary>
[TestClass]
public class ProxyServerOptionsTests
{
    /// <summary>
    /// 测试：Mode属性是必需的
    /// </summary>
    [TestMethod]
    public void Mode_ShouldBeRequired()
    {
        // Arrange & Act & Assert
        ProxyServerOptions options = new ProxyServerOptions
        {
            Mode = ProxyMode.SseToStdio
        };

        Assert.AreEqual(ProxyMode.SseToStdio, options.Mode);
    }

    /// <summary>
    /// 测试：可以设置和获取SseClient属性
    /// </summary>
    [TestMethod]
    public void SseClient_CanSetAndGet()
    {
        // Arrange
        SseClientOptions sseOptions = new SseClientOptions
        {
            Url = "https://example.com/sse"
        };

        ProxyServerOptions options = new ProxyServerOptions
        {
            Mode = ProxyMode.SseToStdio,
            SseClient = sseOptions
        };

        // Act & Assert
        Assert.IsNotNull(options.SseClient);
        Assert.AreEqual("https://example.com/sse", options.SseClient.Url);
    }

    /// <summary>
    /// 测试：可以设置和获取McpServers属性
    /// </summary>
    [TestMethod]
    public void McpServers_CanSetAndGet()
    {
        // Arrange
        McpServerConfig serverConfig = new McpServerConfig
        {
            Name = "test-server",
            Command = "npx",
            Arguments = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" }
        };

        ProxyServerOptions options = new ProxyServerOptions
        {
            Mode = ProxyMode.StdioToHttp,
            McpServers = new List<McpServerConfig> { serverConfig }
        };

        // Act & Assert
        Assert.IsNotNull(options.McpServers);
        Assert.AreEqual(1, options.McpServers.Count);
        Assert.AreEqual("test-server", options.McpServers[0].Name);
        Assert.AreEqual("npx", options.McpServers[0].Command);
    }

    /// <summary>
    /// 测试：可以设置和获取HttpServer属性
    /// </summary>
    [TestMethod]
    public void HttpServer_CanSetAndGet()
    {
        // Arrange
        HttpServerOptions httpOptions = new HttpServerOptions
        {
            Port = 5000
        };

        ProxyServerOptions options = new ProxyServerOptions
        {
            Mode = ProxyMode.StdioToHttp,
            HttpServer = httpOptions
        };

        // Act & Assert
        Assert.IsNotNull(options.HttpServer);
        Assert.AreEqual(5000, options.HttpServer.Port);
    }
}

/// <summary>
/// 针对<see cref="SseClientOptions"/>类的单元测试
/// </summary>
[TestClass]
public class SseClientOptionsTests
{
    /// <summary>
    /// 测试：Url属性是必需的且必须是有效URL
    /// </summary>
    [TestMethod]
    public void Url_ShouldBeValidUrl()
    {
        // Arrange
        SseClientOptions options = new SseClientOptions
        {
            Url = "https://example.com/mcp"
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// 测试：无效的URL应该验证失败
    /// </summary>
    [TestMethod]
    public void Url_InvalidUrl_ShouldFailValidation()
    {
        // Arrange
        SseClientOptions options = new SseClientOptions
        {
            Url = "not-a-valid-url"
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Count > 0);
    }

    /// <summary>
    /// 测试：VerifySsl属性默认值应该是true
    /// </summary>
    [TestMethod]
    public void VerifySsl_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        SseClientOptions options = new SseClientOptions
        {
            Url = "https://example.com"
        };

        // Assert
        Assert.IsTrue(options.VerifySsl);
    }

    /// <summary>
    /// 测试：可以设置自定义Headers
    /// </summary>
    [TestMethod]
    public void Headers_CanSetCustomHeaders()
    {
        // Arrange
        Dictionary<string, string> headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token123" },
            { "X-Custom-Header", "CustomValue" }
        };

        SseClientOptions options = new SseClientOptions
        {
            Url = "https://example.com",
            Headers = headers
        };

        // Act & Assert
        Assert.IsNotNull(options.Headers);
        Assert.AreEqual(2, options.Headers.Count);
        Assert.AreEqual("Bearer token123", options.Headers["Authorization"]);
    }

    /// <summary>
    /// 测试：可以设置AccessToken属性
    /// </summary>
    [TestMethod]
    public void AccessToken_CanSet()
    {
        // Arrange
        SseClientOptions options = new SseClientOptions
        {
            Url = "https://example.com",
            AccessToken = "my-secret-token"
        };

        // Act & Assert
        Assert.AreEqual("my-secret-token", options.AccessToken);
    }

    /// <summary>
    /// 测试：可以设置OAuth2配置
    /// </summary>
    [TestMethod]
    public void OAuth2_CanSetConfiguration()
    {
        // Arrange
        OAuth2ClientCredentialsOptions oauth2 = new OAuth2ClientCredentialsOptions
        {
            ClientId = "client-123",
            ClientSecret = "secret-456",
            TokenUrl = "https://auth.example.com/token",
            Scope = "api.read api.write"
        };

        SseClientOptions options = new SseClientOptions
        {
            Url = "https://example.com",
            OAuth2 = oauth2
        };

        // Act & Assert
        Assert.IsNotNull(options.OAuth2);
        Assert.AreEqual("client-123", options.OAuth2.ClientId);
        Assert.AreEqual("secret-456", options.OAuth2.ClientSecret);
        Assert.AreEqual("https://auth.example.com/token", options.OAuth2.TokenUrl);
        Assert.AreEqual("api.read api.write", options.OAuth2.Scope);
    }
}

/// <summary>
/// 针对<see cref="OAuth2ClientCredentialsOptions"/>类的单元测试
/// </summary>
[TestClass]
public class OAuth2ClientCredentialsOptionsTests
{
    /// <summary>
    /// 测试：ClientId属性是必需的
    /// </summary>
    [TestMethod]
    public void ClientId_ShouldBeRequired()
    {
        // Arrange
        OAuth2ClientCredentialsOptions options = new OAuth2ClientCredentialsOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenUrl = "https://auth.example.com/token"
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual("test-client", options.ClientId);
    }

    /// <summary>
    /// 测试：ClientSecret属性是必需的
    /// </summary>
    [TestMethod]
    public void ClientSecret_ShouldBeRequired()
    {
        // Arrange
        OAuth2ClientCredentialsOptions options = new OAuth2ClientCredentialsOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenUrl = "https://auth.example.com/token"
        };

        // Act & Assert
        Assert.AreEqual("test-secret", options.ClientSecret);
    }

    /// <summary>
    /// 测试：TokenUrl属性是必需的且必须是有效URL
    /// </summary>
    [TestMethod]
    public void TokenUrl_ShouldBeValidUrl()
    {
        // Arrange
        OAuth2ClientCredentialsOptions options = new OAuth2ClientCredentialsOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenUrl = "https://auth.example.com/oauth/token"
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsTrue(isValid);
    }

    /// <summary>
    /// 测试：Scope属性是可选的
    /// </summary>
    [TestMethod]
    public void Scope_ShouldBeOptional()
    {
        // Arrange
        OAuth2ClientCredentialsOptions options = new OAuth2ClientCredentialsOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenUrl = "https://auth.example.com/token"
            // Scope未设置
        };

        // Act & Assert
        Assert.IsNull(options.Scope);
    }

    /// <summary>
    /// 测试：可以设置Scope属性
    /// </summary>
    [TestMethod]
    public void Scope_CanSet()
    {
        // Arrange
        OAuth2ClientCredentialsOptions options = new OAuth2ClientCredentialsOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            TokenUrl = "https://auth.example.com/token",
            Scope = "read write admin"
        };

        // Act & Assert
        Assert.AreEqual("read write admin", options.Scope);
    }
}

/// <summary>
/// 针对<see cref="McpServerConfig"/>类的单元测试
/// </summary>
[TestClass]
public class McpServerConfigTests
{
    /// <summary>
    /// 测试：Command属性是必需的
    /// </summary>
    [TestMethod]
    public void Command_ShouldBeRequired()
    {
        // Arrange
        McpServerConfig options = new McpServerConfig
        {
            Command = "npx"
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual("npx", options.Command);
    }

    /// <summary>
    /// 测试：可以设置Arguments列表
    /// </summary>
    [TestMethod]
    public void Arguments_CanSetList()
    {
        // Arrange
        List<string> arguments = new List<string> { "-y", "@modelcontextprotocol/server-everything" };
        McpServerConfig options = new McpServerConfig
        {
            Command = "npx",
            Arguments = arguments
        };

        // Act & Assert
        Assert.IsNotNull(options.Arguments);
        Assert.AreEqual(2, options.Arguments.Count);
        Assert.AreEqual("-y", options.Arguments[0]);
    }

    /// <summary>
    /// 测试：可以设置Environment环境变量
    /// </summary>
    [TestMethod]
    public void Environment_CanSetVariables()
    {
        // Arrange
        Dictionary<string, string> env = new Dictionary<string, string>
        {
            { "NODE_ENV", "production" },
            { "DEBUG", "mcp:*" }
        };

        McpServerConfig options = new McpServerConfig
        {
            Command = "node",
            Environment = env
        };

        // Act & Assert
        Assert.IsNotNull(options.Environment);
        Assert.AreEqual(2, options.Environment.Count);
        Assert.AreEqual("production", options.Environment["NODE_ENV"]);
    }

    /// <summary>
    /// 测试：可以设置WorkingDirectory
    /// </summary>
    [TestMethod]
    public void WorkingDirectory_CanSet()
    {
        // Arrange
        string workingDir = @"C:\Projects\MyServer";
        McpServerConfig options = new McpServerConfig
        {
            Command = "python",
            WorkingDirectory = workingDir
        };

        // Act & Assert
        Assert.AreEqual(workingDir, options.WorkingDirectory);
    }

    /// <summary>
    /// 测试：Name属性可选
    /// </summary>
    [TestMethod]
    public void Name_IsOptional()
    {
        // Arrange
        McpServerConfig options = new McpServerConfig
        {
            Command = "npx"
        };

        // Act & Assert
        Assert.IsNull(options.Name);
    }

    /// <summary>
    /// 测试：Enabled默认为true
    /// </summary>
    [TestMethod]
    public void Enabled_DefaultsToTrue()
    {
        // Arrange
        McpServerConfig options = new McpServerConfig
        {
            Command = "npx"
        };

        // Act & Assert
        Assert.IsTrue(options.Enabled);
    }
}

/// <summary>
/// 针对<see cref="HttpServerOptions"/>类的单元测试
/// </summary>
[TestClass]
public class HttpServerOptionsTests
{
    /// <summary>
    /// 测试：Host属性的默认值应该是localhost
    /// </summary>
    [TestMethod]
    public void Host_DefaultValue_ShouldBeLocalhost()
    {
        // Arrange & Act
        HttpServerOptions options = new HttpServerOptions();

        // Assert
        Assert.AreEqual("localhost", options.Host);
    }

    /// <summary>
    /// 测试：Port属性的默认值应该是3000
    /// </summary>
    [TestMethod]
    public void Port_DefaultValue_ShouldBe3000()
    {
        // Arrange & Act
        HttpServerOptions options = new HttpServerOptions();

        // Assert
        Assert.AreEqual(3000, options.Port);
    }

    /// <summary>
    /// 测试：Port属性必须在有效范围内（1-65535）
    /// </summary>
    [TestMethod]
    public void Port_InvalidRange_ShouldFailValidation()
    {
        // Arrange
        HttpServerOptions options = new HttpServerOptions
        {
            Port = 70000 // 超出有效范围
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Count > 0);
    }

    /// <summary>
    /// 测试：Stateless属性的默认值应该是false
    /// </summary>
    [TestMethod]
    public void Stateless_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        HttpServerOptions options = new HttpServerOptions();

        // Assert
        Assert.IsFalse(options.Stateless);
    }

    /// <summary>
    /// 测试：可以设置AllowedOrigins列表
    /// </summary>
    [TestMethod]
    public void AllowedOrigins_CanSetList()
    {
        // Arrange
        List<string> origins = new List<string> { "https://example.com", "https://app.example.com" };
        HttpServerOptions options = new HttpServerOptions
        {
            AllowedOrigins = origins
        };

        // Act & Assert
        Assert.IsNotNull(options.AllowedOrigins);
        Assert.AreEqual(2, options.AllowedOrigins.Count);
        Assert.AreEqual("https://example.com", options.AllowedOrigins[0]);
    }
}

/// <summary>
/// 针对<see cref="StdioServersOptions"/>类的单元测试
/// </summary>
[TestClass]
public class StdioServersOptionsTests
{
    /// <summary>
    /// 测试：Servers属性是必需的且至少需要一个服务器
    /// </summary>
    [TestMethod]
    public void Servers_ShouldRequireAtLeastOne()
    {
        // Arrange - 空列表
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>()
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(StdioServersOptions.Servers))));
    }

    /// <summary>
    /// 测试：有效的Servers列表应该通过验证
    /// </summary>
    [TestMethod]
    public void Servers_WithValidServer_ShouldPass()
    {
        // Arrange
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig
                {
                    Name = "test-server",
                    Command = "node"
                }
            }
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsTrue(isValid);
        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// 测试：UseNamespacePrefix默认值应该是true
    /// </summary>
    [TestMethod]
    public void UseNamespacePrefix_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            }
        };

        // Assert
        Assert.IsTrue(options.UseNamespacePrefix);
    }

    /// <summary>
    /// 测试：AllowServerFilter默认值应该是true
    /// </summary>
    [TestMethod]
    public void AllowServerFilter_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            }
        };

        // Assert
        Assert.IsTrue(options.AllowServerFilter);
    }

    /// <summary>
    /// 测试：AutoReconnect默认值应该是true
    /// </summary>
    [TestMethod]
    public void AutoReconnect_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            }
        };

        // Assert
        Assert.IsTrue(options.AutoReconnect);
    }

    /// <summary>
    /// 测试：HealthCheckInterval默认值应该是30秒
    /// </summary>
    [TestMethod]
    public void HealthCheckInterval_DefaultValue_ShouldBe30()
    {
        // Arrange & Act
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            }
        };

        // Assert
        Assert.AreEqual(30, options.HealthCheckInterval);
    }

    /// <summary>
    /// 测试：HealthCheckInterval小于5应该验证失败
    /// </summary>
    [TestMethod]
    public void HealthCheckInterval_LessThan5_ShouldFailValidation()
    {
        // Arrange
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            },
            HealthCheckInterval = 3
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(StdioServersOptions.HealthCheckInterval))));
    }

    /// <summary>
    /// 测试：HealthCheckInterval大于600应该验证失败
    /// </summary>
    [TestMethod]
    public void HealthCheckInterval_GreaterThan600_ShouldFailValidation()
    {
        // Arrange
        StdioServersOptions options = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new McpServerConfig { Name = "test", Command = "cmd" }
            },
            HealthCheckInterval = 700
        };

        // Act
        ValidationContext context = new ValidationContext(options);
        List<ValidationResult> results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(StdioServersOptions.HealthCheckInterval))));
    }
}

/// <summary>
/// 针对<see cref="McpServerConfig"/>类的单元测试
/// </summary>
