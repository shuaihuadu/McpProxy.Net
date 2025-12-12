// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.StdioToSse.WebApi.Models;

/// <summary>
/// 服务器状态响应
/// </summary>
public class ServerStatusResponse
{
    /// <summary>
    /// 服务器列表
    /// </summary>
    public required IReadOnlyList<object> Servers { get; init; }

    /// <summary>
    /// 服务器数量
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
