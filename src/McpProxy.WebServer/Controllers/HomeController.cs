// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace McpProxy.Controllers;

/// <summary>
/// 根端点控制器
/// 提供服务器基本信息和端点导航
/// </summary>
[ApiController]
[Route("/")]
[Produces("application/json")]
public class HomeController : ControllerBase
{
    /// <summary>
    /// 获取服务器基本信息和可用端点
    /// </summary>
    /// <remarks>
    /// 返回 MCP Proxy Server 的版本信息和所有可用的 API 端点
    /// </remarks>
    /// <response code="200">成功返回服务器信息</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetServerInfo()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var version = entryAssembly?.GetName().Version?.ToString() ?? "1.0.0";

        var serverInfo = new
        {
            name = "MCP Proxy Server",
            version = version,
            endpoints = new
            {
                mcp = "/mcp",
                management = "/api/management",
                swagger = "/swagger",
                health = "/api/management/health"
            },
            documentation = new
            {
                swagger = "/swagger",
                readme = "https://github.com/shuaihuadu/McpProxy.Net"
            }
        };

        return this.Ok(serverInfo);
    }
}
