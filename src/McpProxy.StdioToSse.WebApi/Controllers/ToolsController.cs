// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using McpProxy.StdioToSse.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.StdioToSse.WebApi.Controllers;

/// <summary>
/// MCP 工具相关 API
/// </summary>
[ApiController]
[Route("api/mcp/tools")]
[Produces("application/json")]
[Tags("Tools")]
public class ToolsController : ControllerBase
{
    private readonly IStdioToSseService _service;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(IStdioToSseService service, ILogger<ToolsController> logger)
    {
        this._service = service;
        this._logger = logger;
    }

    /// <summary>
    /// 列出所有可用工具
    /// </summary>
    /// <param name="server">服务器名称（可选），用于过滤特定服务器的工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工具列表</returns>
    /// <response code="200">成功返回工具列表</response>
    /// <response code="400">请求参数错误</response>
    [HttpPost("list")]
    [ProducesResponseType(typeof(ListToolsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListToolsResult>> ListToolsAsync(
        [FromQuery] string? server,
        CancellationToken cancellationToken)
    {
        this._logger.LogDebug("Listing tools for server: {Server}", server ?? "all");

        ListToolsResult result = await this._service.ListToolsAsync(server, cancellationToken);

        return this.Ok(result);
    }

    /// <summary>
    /// 调用工具
    /// </summary>
    /// <param name="request">工具调用请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工具执行结果</returns>
    /// <response code="200">工具执行成功</response>
    /// <response code="400">请求参数错误或工具不存在</response>
    /// <response code="500">工具执行失败</response>
    [HttpPost("call")]
    [ProducesResponseType(typeof(CallToolResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CallToolResult>> CallToolAsync(
        [FromBody] CallToolRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return this.BadRequest("Tool name is required");
        }

        this._logger.LogDebug("Calling tool: {ToolName} with arguments: {Arguments}",
            request.Name,
            request.Arguments);

        CallToolResult result = await this._service.CallToolAsync(
            request.Name,
            request.Arguments,
            cancellationToken);

        return this.Ok(result);
    }
}
