// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Models;
using McpProxy.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.StdioToSse.WebApi.Controllers;

/// <summary>
/// MCP 服务器管理相关 API
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Tags("Management")]
public class ManagementController : ControllerBase
{
    private readonly IStdioToSseService _service;
    private readonly ILogger<ManagementController> _logger;

    public ManagementController(IStdioToSseService service, ILogger<ManagementController> logger)
    {
        this._service = service;
        this._logger = logger;
    }

    /// <summary>
    /// 获取所有 MCP 服务器的状态
    /// </summary>
    /// <returns>服务器状态信息</returns>
    /// <response code="200">成功返回服务器状态</response>
    [HttpGet("servers")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServersAsync()
    {
        this._logger.LogDebug("Getting server status");

        IReadOnlyList<ServerStatusInfo> servers = await this._service.GetServerStatusAsync();

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

        ServerCapabilities capabilities = this._service.GetAggregatedCapabilities();

        return this.Ok(capabilities);
    }
}
