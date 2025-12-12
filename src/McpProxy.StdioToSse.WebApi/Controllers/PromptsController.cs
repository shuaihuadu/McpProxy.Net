// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using McpProxy.StdioToSse.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace McpProxy.StdioToSse.WebApi.Controllers;

/// <summary>
/// MCP 提示相关 API
/// </summary>
[ApiController]
[Route("api/mcp/prompts")]
[Produces("application/json")]
[Tags("Prompts")]
public class PromptsController : ControllerBase
{
    private readonly IStdioToSseService _service;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IStdioToSseService service, ILogger<PromptsController> logger)
    {
        this._service = service;
        this._logger = logger;
    }

    /// <summary>
    /// 列出所有可用提示
    /// </summary>
    /// <param name="server">服务器名称（可选），用于过滤特定服务器的提示</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提示列表</returns>
    /// <response code="200">成功返回提示列表</response>
    [HttpPost("list")]
    [ProducesResponseType(typeof(ListPromptsResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListPromptsResult>> ListPromptsAsync(
        [FromQuery] string? server,
        CancellationToken cancellationToken)
    {
        this._logger.LogDebug("Listing prompts for server: {Server}", server ?? "all");

        ListPromptsResult result = await this._service.ListPromptsAsync(server, cancellationToken);

        return this.Ok(result);
    }

    /// <summary>
    /// 获取特定提示
    /// </summary>
    /// <param name="request">提示获取请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提示内容</returns>
    /// <response code="200">成功获取提示</response>
    /// <response code="400">请求参数错误或提示不存在</response>
    [HttpPost("get")]
    [ProducesResponseType(typeof(GetPromptResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetPromptResult>> GetPromptAsync(
        [FromBody] GetPromptRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return this.BadRequest("Prompt name is required");
        }

        this._logger.LogDebug("Getting prompt: {PromptName} with arguments: {Arguments}",
            request.Name,
            request.Arguments);

        GetPromptResult result = await this._service.GetPromptAsync(
            request.Name,
            request.Arguments,
            cancellationToken);

        return this.Ok(result);
    }
}
