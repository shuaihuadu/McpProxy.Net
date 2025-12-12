// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;

namespace McpProxy.StdioToSse.WebApi.Extensions;

/// <summary>
/// MCP 服务器配置扩展方法
/// </summary>
public static class McpServerExtensions
{
    private static IStdioToSseService? _initializedService;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// 添加聚合 MCP 服务器支持（使用 StdioToSseService）
    /// </summary>
    public static IServiceCollection AddStdioToHttpMcpServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // StdioToSseService 已经在其他地方注册，这里只需注册 MCP Server
        services.AddSingleton(sp =>
        {
            var service = sp.GetRequiredService<IStdioToSseService>();
            // 延迟创建 McpServerOptions，在初始化后调用
            return service.CreateAggregatedServerOptions();
        });

        services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = false;
            });

        return services;
    }

    /// <summary>
    /// 映射聚合 MCP 端点（使用 StdioToSseService）
    /// 此方法会确保 StdioToSseService 已初始化
    /// </summary>
    public static async Task<WebApplication> MapStdioToHttpMcpAsync(this WebApplication app)
    {
        await _initLock.WaitAsync();
        try
        {
            var service = app.Services.GetRequiredService<IStdioToSseService>();

            if (_initializedService == null)
            {
                await service.InitializeAsync(app.Lifetime.ApplicationStopping);
                _initializedService = service;
            }
        }
        finally
        {
            _initLock.Release();
        }

        app.MapMcp("/mcp");

        return app;
    }

    /// <summary>
    /// 映射聚合 MCP 端点（同步版本）
    /// </summary>
    public static WebApplication MapStdioToHttpMcp(this WebApplication app)
    {
        return app.MapStdioToHttpMcpAsync().GetAwaiter().GetResult();
    }
}
