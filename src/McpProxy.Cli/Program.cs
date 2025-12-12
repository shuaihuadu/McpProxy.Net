using System.CommandLine;
using System.CommandLine.Invocation;
using McpProxy.Core.Configuration;
using McpProxy.Core.Services;
using McpProxy.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// MCP Proxy CLI 应用程序入口点
/// 支持 stdio→SSE 和 SSE→stdio 两种模式的协议转换
/// </summary>
internal class Program
{
    /// <summary>
    /// 应用程序主入口
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>退出代码</returns>
    private static async Task<int> Main(string[] args)
    {
        // 创建根命令
        RootCommand rootCommand = CreateRootCommand();

        // 执行命令
        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// 创建根命令及其子命令
    /// </summary>
    /// <returns>配置好的根命令</returns>
    private static RootCommand CreateRootCommand()
    {
        RootCommand rootCommand = new RootCommand("MCP Proxy - Protocol conversion tool for Model Context Protocol")
        {
            Name = "mcp-proxy"
        };

        // 添加全局选项
        Option<LogLevel> logLevelOption = new Option<LogLevel>(
            aliases: new[] { "--log-level", "-l" },
            getDefaultValue: () => LogLevel.Information,
            description: "Set the logging level");

        Option<bool> debugOption = new Option<bool>(
            aliases: new[] { "--debug", "-d" },
            getDefaultValue: () => false,
            description: "Enable debug mode");

        rootCommand.AddGlobalOption(logLevelOption);
        rootCommand.AddGlobalOption(debugOption);

        // 添加子命令
        rootCommand.AddCommand(CreateSseToStdioCommand());
        rootCommand.AddCommand(CreateStdioToSseCommand());
        rootCommand.AddCommand(CreateConfigCommand());

        return rootCommand;
    }

    /// <summary>
    /// 创建 SSE→Stdio 模式的命令
    /// </summary>
    /// <returns>SSE到Stdio命令</returns>
    private static Command CreateSseToStdioCommand()
    {
        Command command = new Command("sse-to-stdio", "Connect to remote SSE server and expose via stdio")
        {
            // URL参数（必需）
            new Argument<string>("url", "Remote SSE server URL (e.g., https://api.example.com/sse)")
        };

        // 添加选项
        Option<string?> accessTokenOption = new Option<string?>(
            "--access-token",
            "Bearer token for authentication");

        Option<Dictionary<string, string>?> headersOption = new Option<Dictionary<string, string>?>(
            aliases: new[] { "--header", "-H" },
            description: "Custom HTTP headers (format: Key=Value)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        Option<bool> verifySslOption = new Option<bool>(
            "--verify-ssl",
            getDefaultValue: () => true,
            "Verify SSL certificates");

        Option<string?> clientIdOption = new Option<string?>("--client-id", "OAuth2 client ID");
        Option<string?> clientSecretOption = new Option<string?>("--client-secret", "OAuth2 client secret");
        Option<string?> tokenUrlOption = new Option<string?>("--token-url", "OAuth2 token endpoint URL");
        Option<string?> scopeOption = new Option<string?>("--scope", "OAuth2 scope");

        command.AddOption(accessTokenOption);
        command.AddOption(headersOption);
        command.AddOption(verifySslOption);
        command.AddOption(clientIdOption);
        command.AddOption(clientSecretOption);
        command.AddOption(tokenUrlOption);
        command.AddOption(scopeOption);

        // 设置处理器
        command.SetHandler(async (InvocationContext context) =>
        {
            string url = context.ParseResult.GetValueForArgument((Argument<string>)command.Arguments[0]);
            string? accessToken = context.ParseResult.GetValueForOption(accessTokenOption);
            Dictionary<string, string>? headers = context.ParseResult.GetValueForOption(headersOption);
            bool verifySsl = context.ParseResult.GetValueForOption(verifySslOption);
            string? clientId = context.ParseResult.GetValueForOption(clientIdOption);
            string? clientSecret = context.ParseResult.GetValueForOption(clientSecretOption);
            string? tokenUrl = context.ParseResult.GetValueForOption(tokenUrlOption);
            string? scope = context.ParseResult.GetValueForOption(scopeOption);
            LogLevel logLevel = context.ParseResult.GetValueForOption((Option<LogLevel>)context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "log-level"));

            await RunSseToStdioAsync(url, accessToken, headers, verifySsl, clientId, clientSecret, tokenUrl, scope, logLevel, context.GetCancellationToken()).ConfigureAwait(false);
        });

        return command;
    }

    /// <summary>
    /// 创建 Stdio→SSE 模式的命令
    /// </summary>
    /// <returns>Stdio到SSE命令</returns>
    private static Command CreateStdioToSseCommand()
    {
        Command command = new Command("stdio-to-sse", "Start SSE server and proxy to stdio MCP server(s)")
        {
            // 命令参数（MCP服务器命令）
            new Argument<string[]>("command", "MCP server command and arguments")
        };

        // 添加选项
        Option<int> portOption = new Option<int>(
            "--port",
            getDefaultValue: () => 3000,
            "HTTP server port");

        Option<string> hostOption = new Option<string>(
            "--host",
            getDefaultValue: () => "localhost",
            "HTTP server host");

        Option<bool> statelessOption = new Option<bool>(
            "--stateless",
            getDefaultValue: () => false,
            "Enable stateless mode");

        Option<string[]?> allowOriginOption = new Option<string[]?>(
            "--allow-origin",
            "Allowed CORS origins")
        {
            AllowMultipleArgumentsPerToken = true
        };

        Option<Dictionary<string, string>?> envOption = new Option<Dictionary<string, string>?>(
            aliases: new[] { "--env", "-e" },
            description: "Environment variables (format: KEY=VALUE)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        Option<string?> cwdOption = new Option<string?>("--cwd", "Working directory");

        Option<bool> useNamespacePrefixOption = new Option<bool>(
            "--use-namespace-prefix",
            getDefaultValue: () => true,
            "Use namespace prefix for multi-server mode");

        command.AddOption(portOption);
        command.AddOption(hostOption);
        command.AddOption(statelessOption);
        command.AddOption(allowOriginOption);
        command.AddOption(envOption);
        command.AddOption(cwdOption);
        command.AddOption(useNamespacePrefixOption);

        // 设置处理器
        command.SetHandler(async (InvocationContext context) =>
        {
            string[] serverCommand = context.ParseResult.GetValueForArgument((Argument<string[]>)command.Arguments[0]);
            int port = context.ParseResult.GetValueForOption(portOption);
            string host = context.ParseResult.GetValueForOption(hostOption);
            bool stateless = context.ParseResult.GetValueForOption(statelessOption);
            string[]? allowOrigin = context.ParseResult.GetValueForOption(allowOriginOption);
            Dictionary<string, string>? env = context.ParseResult.GetValueForOption(envOption);
            string? cwd = context.ParseResult.GetValueForOption(cwdOption);
            bool useNamespacePrefix = context.ParseResult.GetValueForOption(useNamespacePrefixOption);
            LogLevel logLevel = context.ParseResult.GetValueForOption((Option<LogLevel>)context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "log-level"));

            await RunStdioToSseAsync(serverCommand, port, host, stateless, allowOrigin, env, cwd, useNamespacePrefix, logLevel, context.GetCancellationToken()).ConfigureAwait(false);
        });

        return command;
    }

    /// <summary>
    /// 创建基于配置文件运行的命令
    /// </summary>
    /// <returns>配置命令</returns>
    private static Command CreateConfigCommand()
    {
        Command command = new Command("config", "Run using configuration file")
        {
            new Argument<string>(
                "config-file",
                getDefaultValue: () => "appsettings.json",
                "Path to configuration file")
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            string configFile = context.ParseResult.GetValueForArgument((Argument<string>)command.Arguments[0]);
            LogLevel logLevel = context.ParseResult.GetValueForOption((Option<LogLevel>)context.ParseResult.RootCommandResult.Command.Options.First(o => o.Name == "log-level"));

            await RunFromConfigAsync(configFile, logLevel, context.GetCancellationToken()).ConfigureAwait(false);
        });

        return command;
    }

    /// <summary>
    /// 运行 SSE→Stdio 模式
    /// </summary>
    private static async Task RunSseToStdioAsync(
        string url,
        string? accessToken,
        Dictionary<string, string>? headers,
        bool verifySsl,
        string? clientId,
        string? clientSecret,
        string? tokenUrl,
        string? scope,
        LogLevel logLevel,
        CancellationToken cancellationToken)
    {
        // 构建配置
        SseClientOptions sseOptions = new SseClientOptions
        {
            Url = url,
            AccessToken = accessToken,
            Headers = headers,
            VerifySsl = verifySsl
        };

        // 如果提供了OAuth2参数，添加OAuth2配置
        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tokenUrl))
        {
            sseOptions.OAuth2 = new OAuth2ClientCredentialsOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                TokenUrl = tokenUrl,
                Scope = scope
            };
        }

        // 创建主机
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        ConfigureLogging(builder.Logging, logLevel);

        builder.Services.Configure<SseClientOptions>(options =>
        {
            options.Url = sseOptions.Url;
            options.AccessToken = sseOptions.AccessToken;
            options.Headers = sseOptions.Headers;
            options.VerifySsl = sseOptions.VerifySsl;
            options.OAuth2 = sseOptions.OAuth2;
        });

        builder.Services.AddSingleton<ISseToStdioService, SseToStdioProxyService>();

        IHost app = builder.Build();
        ISseToStdioService service = app.Services.GetRequiredService<ISseToStdioService>();

        Console.WriteLine($"?? Connecting to SSE server: {url}");
        Console.WriteLine("Press Ctrl+C to stop");

        try
        {
            await service.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n? Service stopped gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 运行 Stdio→SSE 模式
    /// </summary>
    private static async Task RunStdioToSseAsync(
        string[] serverCommand,
        int port,
        string host,
        bool stateless,
        string[]? allowOrigin,
        Dictionary<string, string>? env,
        string? cwd,
        bool useNamespacePrefix,
        LogLevel logLevel,
        CancellationToken cancellationToken)
    {
        if (serverCommand.Length == 0)
        {
            Console.WriteLine("? Error: Server command is required");
            return;
        }

        // 构建配置
        McpServerConfig namedServer = new McpServerConfig
        {
            Name = "default",
            Command = serverCommand[0],
            Arguments = serverCommand.Length > 1 ? serverCommand.Skip(1).ToList() : new List<string>(),
            Environment = env,
            WorkingDirectory = cwd,
            Enabled = true
        };

        StdioServersOptions stdioOptions = new StdioServersOptions
        {
            Servers = new List<McpServerConfig> { namedServer },
            UseNamespacePrefix = useNamespacePrefix,
            AllowServerFilter = true,
            AutoReconnect = true,
            HealthCheckInterval = 30
        };

        HttpServerOptions httpOptions = new HttpServerOptions
        {
            Host = host,
            Port = port,
            Stateless = stateless,
            AllowedOrigins = allowOrigin?.ToList()
        };

        // 创建主机
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        ConfigureLogging(builder.Logging, logLevel);

        builder.Services.Configure<StdioServersOptions>(options =>
        {
            options.Servers = stdioOptions.Servers;
            options.UseNamespacePrefix = stdioOptions.UseNamespacePrefix;
            options.AllowServerFilter = stdioOptions.AllowServerFilter;
            options.AutoReconnect = stdioOptions.AutoReconnect;
            options.HealthCheckInterval = stdioOptions.HealthCheckInterval;
        });

        builder.Services.Configure<HttpServerOptions>(options =>
        {
            options.Host = httpOptions.Host;
            options.Port = httpOptions.Port;
            options.Stateless = httpOptions.Stateless;
            options.AllowedOrigins = httpOptions.AllowedOrigins;
        });

        builder.Services.AddSingleton<IProxyService, StdioToHttpProxyService>();

        IHost app = builder.Build();
        IProxyService service = app.Services.GetRequiredService<IProxyService>();

        Console.WriteLine($"?? Starting SSE server on http://{host}:{port}");
        Console.WriteLine($"?? Proxying to: {string.Join(" ", serverCommand)}");
        Console.WriteLine("Press Ctrl+C to stop");

        try
        {
            await service.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n? Service stopped gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从配置文件运行
    /// </summary>
    private static async Task RunFromConfigAsync(
        string configFile,
        LogLevel logLevel,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(configFile))
        {
            Console.WriteLine($"? Configuration file not found: {configFile}");
            return;
        }

        Console.WriteLine($"?? Loading configuration from: {configFile}");

        // 创建主机
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        ConfigureLogging(builder.Logging, logLevel);

        // 加载配置
        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonFile(configFile, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "MCPPROXY_");

        // 获取配置
        ProxyServerOptions? proxyConfig = builder.Configuration.Get<ProxyServerOptions>();

        if (proxyConfig == null)
        {
            Console.WriteLine("? Invalid configuration file");
            return;
        }

        builder.Services.Configure<ProxyServerOptions>(builder.Configuration);

        // 根据模式注册服务
        if (proxyConfig.Mode == ProxyMode.SseToStdio)
        {
            if (proxyConfig.SseClient == null)
            {
                Console.WriteLine("? SSE client configuration is required for SseToStdio mode");
                return;
            }

            builder.Services.Configure<SseClientOptions>(
                builder.Configuration.GetSection(nameof(ProxyServerOptions.SseClient)));
            builder.Services.AddSingleton<IProxyService, SseToStdioProxyService>();

            Console.WriteLine($"?? Starting in SSE→Stdio mode");
            Console.WriteLine($"?? Connecting to: {proxyConfig.SseClient.Url}");
        }
        else if (proxyConfig.Mode == ProxyMode.StdioToHttp)
        {
            if (proxyConfig.HttpServer == null || proxyConfig.McpServers == null || proxyConfig.McpServers.Count == 0)
            {
                Console.WriteLine("? HTTP server and MCP servers configuration required for StdioToHttp mode");
                return;
            }

            // 自动分配服务器名称
            for (int i = 0; i < proxyConfig.McpServers.Count; i++)
            {
                McpServerConfig server = proxyConfig.McpServers[i];
                if (string.IsNullOrEmpty(server.Name))
                {
                    server.Name = proxyConfig.McpServers.Count == 1 ? "default" : $"server{i + 1}";
                }
            }

            // 构建StdioServersOptions
            StdioServersOptions stdioOptions = new StdioServersOptions
            {
                Servers = proxyConfig.McpServers.ToList(), // 直接使用已有的配置
                UseNamespacePrefix = proxyConfig.McpServers.Count > 1 && proxyConfig.UseNamespacePrefix,
                AllowServerFilter = proxyConfig.AllowServerFilter,
                AutoReconnect = proxyConfig.AutoReconnect,
                HealthCheckInterval = proxyConfig.HealthCheckInterval
            };

            builder.Services.Configure<StdioServersOptions>(options =>
            {
                options.Servers = stdioOptions.Servers;
                options.UseNamespacePrefix = stdioOptions.UseNamespacePrefix;
                options.AllowServerFilter = stdioOptions.AllowServerFilter;
                options.AutoReconnect = stdioOptions.AutoReconnect;
                options.HealthCheckInterval = stdioOptions.HealthCheckInterval;
            });

            builder.Services.Configure<HttpServerOptions>(
                builder.Configuration.GetSection(nameof(ProxyServerOptions.HttpServer)));
            builder.Services.AddSingleton<IProxyService, StdioToHttpProxyService>();

            Console.WriteLine($"?? Starting in Stdio→SSE mode");
            Console.WriteLine($"?? HTTP server: http://{proxyConfig.HttpServer.Host}:{proxyConfig.HttpServer.Port}");
            Console.WriteLine($"?? Servers: {string.Join(", ", proxyConfig.McpServers.Select(s => s.Name))}");
        }
        else
        {
            Console.WriteLine($"? Unsupported proxy mode: {proxyConfig.Mode}");
            return;
        }

        Console.WriteLine("Press Ctrl+C to stop");

        IHost app = builder.Build();
        IProxyService service = app.Services.GetRequiredService<IProxyService>();

        try
        {
            await service.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n? Service stopped gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n? Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 配置日志记录
    /// </summary>
    /// <param name="logging">日志构建器</param>
    /// <param name="logLevel">日志级别</param>
    private static void ConfigureLogging(ILoggingBuilder logging, LogLevel logLevel)
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(logLevel);
    }
}
