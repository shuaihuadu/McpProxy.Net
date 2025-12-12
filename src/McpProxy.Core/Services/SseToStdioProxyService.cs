using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using McpProxy.Core.Authentication;
using McpProxy.Core.Configuration;
using McpProxy.Abstractions.Services;

namespace McpProxy.Core.Services;

/// <summary>
/// 实现从SSE到Stdio的代理服务
/// 该服务连接到远程SSE MCP服务器并通过Stdio方式暴露
/// </summary>
public sealed class SseToStdioProxyService : ISseToStdioService, IProxyService
{
    private readonly SseClientOptions _options;
    private readonly ILogger<SseToStdioProxyService> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// 初始化 <see cref="SseToStdioProxyService"/> 类的新实例
    /// </summary>
    /// <param name="options">SSE客户端配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="loggerFactory">日志工厂（可选）</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="options"/> 或 <paramref name="logger"/> 为 null 时抛出</exception>
    public SseToStdioProxyService(
        IOptions<SseClientOptions> options,
        ILogger<SseToStdioProxyService> logger,
        ILoggerFactory? loggerFactory = null)
    {
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Starting SSE to Stdio proxy: connecting to {Url}", this._options.Url);

        // 创建HTTP客户端传输以连接到远程SSE服务器
        HttpClientTransportOptions transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(this._options.Url),
            TransportMode = HttpTransportMode.Sse,
            Name = "SSE Client"
        };

        // 配置HttpClient（包括头信息和SSL验证）
        using HttpClient httpClient = this.CreateHttpClient();
        await using HttpClientTransport clientTransport = new HttpClientTransport(
            transportOptions,
            httpClient,
            loggerFactory: this._loggerFactory,
            ownsHttpClient: false);

        // 连接到远程服务器
        this._logger.LogInformation("Connecting to remote SSE server...");
        await using McpClient mcpClient = await McpClient.CreateAsync(
            clientTransport,
            clientOptions: new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "mcp-proxy",
                    Version = "1.0.0"
                }
            },
            loggerFactory: this._loggerFactory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        string serverName = mcpClient.ServerInfo?.Name ?? "Unknown";
        this._logger.LogInformation("Connected to remote server: {ServerName}", serverName);

        // 创建代理服务器，将所有请求转发到远程客户端
        McpServerOptions serverOptions = this.CreateProxyServerOptions(mcpClient);

        // 使用stdio传输暴露代理
        StdioServerTransport stdioTransport = new StdioServerTransport(serverOptions);

        this._logger.LogInformation("Starting stdio server...");
        await using McpServer proxyServer = McpServer.Create(stdioTransport, serverOptions, this._loggerFactory);
        await proxyServer.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 创建配置了自定义设置的HttpClient
    /// </summary>
    /// <returns>配置好的HttpClient实例</returns>
    private HttpClient CreateHttpClient()
    {
        HttpClientHandler handler = new HttpClientHandler();

        // 如果禁用SSL验证，则配置证书验证回调
        if (!this._options.VerifySsl)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            this._logger.LogWarning("SSL certificate validation is disabled");
        }

        // 检查是否配置了OAuth2认证
        HttpMessageHandler finalHandler = handler;
        if (this._options.OAuth2 != null)
        {
            this._logger.LogInformation(
                "Configuring OAuth2 Client Credentials authentication for {TokenUrl}",
                this._options.OAuth2.TokenUrl);

            OAuth2ClientCredentialsHandler oauth2Handler = new OAuth2ClientCredentialsHandler(
                this._options.OAuth2.ClientId,
                this._options.OAuth2.ClientSecret,
                this._options.OAuth2.TokenUrl,
                this._options.OAuth2.Scope)
            {
                InnerHandler = handler
            };
            finalHandler = oauth2Handler;
        }

        HttpClient client = new HttpClient(finalHandler);

        // 添加自定义HTTP头
        if (this._options.Headers != null)
        {
            foreach (KeyValuePair<string, string> header in this._options.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // 处理Bearer Token认证（优先级：配置文件 > 环境变量）
        string? accessToken = this._options.AccessToken
            ?? Environment.GetEnvironmentVariable("API_ACCESS_TOKEN");

        if (!string.IsNullOrEmpty(accessToken))
        {
            this._logger.LogInformation("Adding Bearer token authentication");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    /// <summary>
    /// 创建代理服务器选项，配置请求处理器
    /// </summary>
    /// <param name="remoteClient">远程MCP客户端</param>
    /// <returns>配置好的服务器选项</returns>
    private McpServerOptions CreateProxyServerOptions(McpClient remoteClient)
    {
        // 获取远程服务器能力
        Implementation? serverInfo = remoteClient.ServerInfo;
        ServerCapabilities? capabilities = remoteClient.ServerCapabilities;

        this._logger.LogInformation(
            "Remote server capabilities: Tools={HasTools}, Resources={HasResources}, Prompts={HasPrompts}",
            capabilities?.Tools != null,
            capabilities?.Resources != null,
            capabilities?.Prompts != null);

        McpServerOptions options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = $"proxy-{serverInfo?.Name ?? "unknown"}",
                Version = serverInfo?.Version ?? "1.0.0"
            },
            Capabilities = capabilities ?? new ServerCapabilities(),
            Handlers = new McpServerHandlers()
        };

        // 如果远程支持工具，则注册工具处理器
        if (capabilities?.Tools != null)
        {
            this.RegisterToolHandlers(options, remoteClient);
        }

        // 如果远程支持提示，则注册提示处理器
        if (capabilities?.Prompts != null)
        {
            this.RegisterPromptHandlers(options, remoteClient);
        }

        // 如果远程支持资源，则注册资源处理器
        if (capabilities?.Resources != null)
        {
            this.RegisterResourceHandlers(options, remoteClient);
        }

        return options;
    }

    /// <summary>
    /// 注册工具相关的请求处理器
    /// </summary>
    /// <param name="options">服务器选项</param>
    /// <param name="remoteClient">远程MCP客户端</param>
    private void RegisterToolHandlers(McpServerOptions options, McpClient remoteClient)
    {
        // 列出工具处理器
        options.Handlers.ListToolsHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding list_tools request");
            ListToolsResult tools = await remoteClient.ListToolsAsync(
                request.Params ?? new ListToolsRequestParams(),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new ListToolsResult { Tools = tools.Tools };
        };

        // 调用工具处理器
        options.Handlers.CallToolHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding call_tool request: {ToolName}", request.Params?.Name);
            CallToolResult result = await remoteClient.CallToolAsync(
                request.Params!,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        };
    }

    /// <summary>
    /// 注册提示相关的请求处理器
    /// </summary>
    /// <param name="options">服务器选项</param>
    /// <param name="remoteClient">远程MCP客户端</param>
    private void RegisterPromptHandlers(McpServerOptions options, McpClient remoteClient)
    {
        // 列出提示处理器
        options.Handlers.ListPromptsHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding list_prompts request");
            ListPromptsResult promptsResult = await remoteClient.ListPromptsAsync(
                request.Params ?? new ListPromptsRequestParams(),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new ListPromptsResult { Prompts = promptsResult.Prompts };
        };

        // 获取提示处理器
        options.Handlers.GetPromptHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding get_prompt request: {PromptName}", request.Params?.Name);
            GetPromptResult result = await remoteClient.GetPromptAsync(
                request.Params!,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        };
    }

    /// <summary>
    /// 注册资源相关的请求处理器
    /// </summary>
    /// <param name="options">服务器选项</param>
    /// <param name="remoteClient">远程MCP客户端</param>
    private void RegisterResourceHandlers(McpServerOptions options, McpClient remoteClient)
    {
        // 列出资源处理器
        options.Handlers.ListResourcesHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding list_resources request");
            ListResourcesResult resourcesResult = await remoteClient.ListResourcesAsync(
                request.Params ?? new ListResourcesRequestParams(),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new ListResourcesResult { Resources = resourcesResult.Resources };
        };

        // 读取资源处理器
        options.Handlers.ReadResourceHandler = async (request, cancellationToken) =>
        {
            this._logger.LogDebug("Forwarding read_resource request: {Uri}", request.Params?.Uri);
            ReadResourceResult result = await remoteClient.ReadResourceAsync(
                request.Params!,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        };
    }
}
