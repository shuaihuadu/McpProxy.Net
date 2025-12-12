// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.StdioToSse.WebApi.Controllers;

/// <summary>
/// MCP 代理服务器管理 API
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Tags("Management")]
public class ManagementController : ControllerBase
{
    private readonly IStdioToSseService _service;
    private readonly ILogger<ManagementController> _logger;

    public ManagementController(
        IStdioToSseService service,
        ILogger<ManagementController> logger)
    {
        this._service = service;
        this._logger = logger;
    }

    /// <summary>
    /// 获取所有后端 MCP 服务器状态
    /// </summary>
    /// <returns>服务器状态信息</returns>
    /// <response code="200">成功返回服务器状态</response>
    [HttpGet("servers")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServersAsync()
    {
        this._logger.LogDebug("Getting server status");

        var serverStatus = await this._service.GetServerStatusAsync();
        var servers = serverStatus.Select(s => new
        {
            name = s.Name,
            connected = s.IsConnected,
            serverInfo = s.ServerName,
            version = s.ServerVersion,
            lastHeartbeat = s.LastHeartbeat,
            capabilities = new
            {
                tools = s.Capabilities?.Tools != null,
                prompts = s.Capabilities?.Prompts != null,
                resources = s.Capabilities?.Resources != null
            }
        }).ToList();

        return this.Ok(new
        {
            servers,
            count = servers.Count,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取聚合的服务器能力
    /// </summary>
    /// <returns>聚合的服务器能力</returns>
    /// <response code="200">成功返回服务器能力</response>
    [HttpGet("capabilities")]
    [ProducesResponseType(typeof(ServerCapabilities), StatusCodes.Status200OK)]
    public IActionResult GetCapabilities()
    {
        this._logger.LogDebug("Getting aggregated capabilities");

        var capabilities = this._service.GetAggregatedCapabilities();

        return this.Ok(capabilities);
    }
}
