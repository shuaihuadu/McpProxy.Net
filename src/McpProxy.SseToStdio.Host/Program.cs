using McpProxy.Core.Configuration;
using McpProxy.Core.Services;
using McpProxy.Abstractions.Services;
using McpProxy.SseToStdio.Host.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 加载配置
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(prefix: "MCPPROXY_")
    .AddCommandLine(args);

// 配置选项
builder.Services.Configure<SseClientOptions>(
    builder.Configuration.GetSection("SseClient"));

// 注册核心服务
builder.Services.AddSingleton<ISseToStdioService, SseToStdioProxyService>();

// 注册后台服务
builder.Services.AddHostedService<SseToStdioWorker>();

// 配置为 Windows Service (可选)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "McpProxy.SseToStdio";
});

// 配置为 Linux systemd (可选)
builder.Services.AddSystemd();

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

IHost host = builder.Build();

await host.RunAsync();
