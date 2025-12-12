// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using McpProxy.StdioToSse.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.StdioToSse.WebApi.Controllers;

/// <summary>
/// MCP 资源相关 API
/// </summary>
[ApiController]
[Route("api/mcp/resources")]
[Produces("application/json")]
[Tags("Resources")]
public class ResourcesController : ControllerBase
{
    private readonly IStdioToSseService _service;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(IStdioToSseService service, ILogger<ResourcesController> logger)
    {
        this._service = service;
        this._logger = logger;
    }

    /// <summary>
    /// 列出所有可用资源
    /// </summary>
    /// <param name="server">服务器名称（可选），用于过滤特定服务器的资源</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源列表</returns>
    /// <response code="200">成功返回资源列表</response>
    [HttpPost("list")]
    [ProducesResponseType(typeof(ListResourcesResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResourcesResult>> ListResourcesAsync(
        [FromQuery] string? server,
        CancellationToken cancellationToken)
    {
        this._logger.LogDebug("Listing resources for server: {Server}", server ?? "all");

        ListResourcesResult result = await this._service.ListResourcesAsync(server, cancellationToken);

        return this.Ok(result);
    }

    /// <summary>
    /// 读取特定资源
    /// </summary>
    /// <param name="request">资源读取请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源内容</returns>
    /// <response code="200">成功读取资源</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="404">资源不存在</response>
    [HttpPost("read")]
    [ProducesResponseType(typeof(ReadResourceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadResourceResult>> ReadResourceAsync(
        [FromBody] ReadResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Uri))
        {
            return this.BadRequest("Resource URI is required");
        }

        this._logger.LogDebug("Reading resource: {ResourceUri}", request.Uri);

        ReadResourceResult result = await this._service.ReadResourceAsync(
            request.Uri,
            cancellationToken);

        return this.Ok(result);
    }
}
