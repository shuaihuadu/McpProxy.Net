// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.StdioToSse.WebApi.Models;

/// <summary>
/// 调用工具的请求
/// </summary>
/// <param name="Name">工具名称（可能包含服务器前缀，例如 "server1:toolname"）</param>
/// <param name="Arguments">工具参数（可选）</param>
public record CallToolRequest(string Name, object? Arguments = null);

/// <summary>
/// 获取提示的请求
/// </summary>
/// <param name="Name">提示名称</param>
/// <param name="Arguments">提示参数（可选）</param>
public record GetPromptRequest(string Name, object? Arguments = null);

/// <summary>
/// 读取资源的请求
/// </summary>
/// <param name="Uri">资源 URI</param>
public record ReadResourceRequest(string Uri);
