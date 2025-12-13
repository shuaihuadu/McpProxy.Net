// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.Controllers;

/// <summary>
/// MCP 服务器管理 API
/// 提供服务器状态查询、健康检查、刷新等管理功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ManagementController : ControllerBase
{
    private readonly IMcpProxyService _proxyService;
    private readonly ILogger<ManagementController> _logger;

    /// <summary>
    /// 初始化 MCP 管理控制器
    /// </summary>
    /// <param name="proxyService">MCP 代理服务</param>
    /// <param name="logger">日志记录器</param>
    public ManagementController(
        IMcpProxyService proxyService,
        ILogger<ManagementController> logger)
    {
        this._proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取服务状态信息
    /// </summary>
    /// <remarks>
    /// 返回当前 MCP 服务的初始化状态、服务器数量、工具/提示词/资源统计等信息
    /// </remarks>
    /// <response code="200">成功返回服务状态</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ServiceStatus), StatusCodes.Status200OK)]
    public ActionResult<ServiceStatus> GetStatus()
    {
        try
        {
            this._logger.LogInformation("Getting MCP service status");

            ServiceStatus status = this._proxyService.GetStatus();

            return this.Ok(status);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting service status");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to get service status",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 验证所有服务器的健康状态
    /// </summary>
    /// <remarks>
    /// 检查每个 MCP 服务器的连接状态，返回详细的健康检查结果
    /// </remarks>
    /// <response code="200">健康检查完成</response>
    /// <response code="503">存在不健康的服务器</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthCheckResult>> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Starting health check for all MCP servers");

            HealthCheckResult health = await this._proxyService.ValidateAsync(cancellationToken);

            this._logger.LogInformation(
                "Health check completed. Healthy: {HealthyCount}, Unhealthy: {UnhealthyCount}",
                health.HealthyServers,
                health.UnhealthyServers);

            if (!health.IsHealthy)
            {
                return this.StatusCode(StatusCodes.Status503ServiceUnavailable, health);
            }

            return this.Ok(health);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during health check");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Health check failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 刷新服务器缓存
    /// </summary>
    /// <remarks>
    /// 重新发现所有 MCP 服务器，重建工具、提示词和资源的映射关系。
    /// 适用于配置文件或数据库更改后，或需要手动刷新服务器列表时。
    /// </remarks>
    /// <response code="200">刷新成功</response>
    /// <response code="500">刷新失败</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Starting manual refresh of MCP service");

            ServiceStatus beforeStatus = this._proxyService.GetStatus();

            await this._proxyService.RefreshAsync(cancellationToken);

            ServiceStatus afterStatus = this._proxyService.GetStatus();

            this._logger.LogInformation(
                "Refresh completed. Servers: {BeforeCount} → {AfterCount}, Tools: {BeforeTools} → {AfterTools}",
                beforeStatus.TotalServers,
                afterStatus.TotalServers,
                beforeStatus.TotalTools,
                afterStatus.TotalTools);

            return this.Ok(new
            {
                message = "MCP service refreshed successfully",
                before = new
                {
                    servers = beforeStatus.TotalServers,
                    tools = beforeStatus.TotalTools,
                    prompts = beforeStatus.TotalPrompts,
                    resources = beforeStatus.TotalResources
                },
                after = new
                {
                    servers = afterStatus.TotalServers,
                    tools = afterStatus.TotalTools,
                    prompts = afterStatus.TotalPrompts,
                    resources = afterStatus.TotalResources,
                    serverNames = afterStatus.ServerNames
                },
                refreshedAt = afterStatus.LastInitializedAt
            });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during refresh operation");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Refresh failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 列出所有可用的工具
    /// </summary>
    /// <param name="serverName">可选的服务器名称筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 返回所有或指定服务器的工具列表，用于调试和查看
    /// </remarks>
    /// <response code="200">成功返回工具列表</response>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ListToolsAsync(
        [FromQuery] string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger.LogInformation("Listing tools. Server filter: {ServerName}", serverName ?? "ALL");

            ListToolsResult result = await this._proxyService.ListToolsAsync(serverName, null, cancellationToken);

            var toolsInfo = result.Tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                hasInputSchema = t.InputSchema.ValueKind != System.Text.Json.JsonValueKind.Undefined
            }).ToList();

            return this.Ok(new
            {
                totalCount = result.Tools.Count,
                serverFilter = serverName,
                tools = toolsInfo
            });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error listing tools");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to list tools",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 列出所有可用的提示词
    /// </summary>
    /// <param name="serverName">可选的服务器名称筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 返回所有或指定服务器的提示词列表
    /// </remarks>
    /// <response code="200">成功返回提示词列表</response>
    [HttpGet("prompts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ListPromptsAsync(
        [FromQuery] string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger.LogInformation("Listing prompts. Server filter: {ServerName}", serverName ?? "ALL");

            ListPromptsResult result = await this._proxyService.ListPromptsAsync(serverName, null, cancellationToken);

            var promptsInfo = result.Prompts.Select(p => new
            {
                name = p.Name,
                description = p.Description,
                argumentCount = p.Arguments?.Count ?? 0
            }).ToList();

            return this.Ok(new
            {
                totalCount = result.Prompts.Count,
                serverFilter = serverName,
                prompts = promptsInfo
            });
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Prompts feature not implemented");
            return this.StatusCode(StatusCodes.Status501NotImplemented, new
            {
                error = "Prompts feature not implemented"
            });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error listing prompts");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to list prompts",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 列出所有可用的资源
    /// </summary>
    /// <param name="serverName">可选的服务器名称筛选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 返回所有或指定服务器的资源列表
    /// </remarks>
    /// <response code="200">成功返回资源列表</response>
    [HttpGet("resources")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ListResourcesAsync(
        [FromQuery] string? serverName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            this._logger.LogInformation("Listing resources. Server filter: {ServerName}", serverName ?? "ALL");

            ListResourcesResult result = await this._proxyService.ListResourcesAsync(serverName, null, cancellationToken);

            var resourcesInfo = result.Resources.Select(r => new
            {
                uri = r.Uri,
                name = r.Name,
                description = r.Description,
                mimeType = r.MimeType
            }).ToList();

            return this.Ok(new
            {
                totalCount = result.Resources.Count,
                serverFilter = serverName,
                resources = resourcesInfo
            });
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Resources feature not implemented");
            return this.StatusCode(StatusCodes.Status501NotImplemented, new
            {
                error = "Resources feature not implemented"
            });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error listing resources");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to list resources",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取完整的调试信息
    /// </summary>
    /// <remarks>
    /// 返回服务状态、健康检查、工具/提示词/资源统计等完整信息，用于调试
    /// </remarks>
    /// <response code="200">成功返回调试信息</response>
    [HttpGet("debug")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetDebugInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation("Getting complete debug information");

            ServiceStatus status = this._proxyService.GetStatus();
            HealthCheckResult health = await this._proxyService.ValidateAsync(cancellationToken);

            var debugInfo = new
            {
                timestamp = DateTime.UtcNow,
                status = new
                {
                    isInitialized = status.IsInitialized,
                    lastInitializedAt = status.LastInitializedAt,
                    totalServers = status.TotalServers,
                    totalTools = status.TotalTools,
                    totalPrompts = status.TotalPrompts,
                    totalResources = status.TotalResources,
                    serverNames = status.ServerNames
                },
                health = new
                {
                    isHealthy = health.IsHealthy,
                    healthyServers = health.HealthyServers,
                    unhealthyServers = health.UnhealthyServers,
                    servers = health.ServerHealths.Select(kvp => new
                    {
                        name = kvp.Key,
                        isConnected = kvp.Value.IsConnected,
                        errorMessage = kvp.Value.ErrorMessage,
                        lastCheckTime = kvp.Value.LastCheckTime
                    }).ToList()
                },
                environment = new
                {
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processId = Environment.ProcessId,
                    dotnetVersion = Environment.Version.ToString()
                }
            };

            return this.Ok(debugInfo);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error getting debug information");
            return this.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Failed to get debug information",
                message = ex.Message
            });
        }
    }
}
