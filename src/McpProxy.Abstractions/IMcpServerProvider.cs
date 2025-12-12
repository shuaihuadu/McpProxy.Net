// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.Abstractions;

/// <summary>
/// MCP 服务器提供者接口
/// 负责提供可用的 MCP 服务器配置列表
/// </summary>
public interface IMcpServerProvider
{
    /// <summary>
    /// 获取所有可用的 MCP 服务器配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务器配置列表</returns>
    Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据服务器名称获取配置
    /// </summary>
    /// <param name="name">服务器名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务器配置，如果不存在则返回 null</returns>
    Task<IMcpServerConfiguration?> GetServerByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据服务器 ID 获取配置
    /// </summary>
    /// <param name="id">服务器 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务器配置，如果不存在则返回 null</returns>
    Task<IMcpServerConfiguration?> GetServerByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据标签过滤服务器
    /// </summary>
    /// <param name="tags">标签列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配标签的服务器列表</returns>
    Task<IReadOnlyList<IMcpServerConfiguration>> GetServersByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 监听服务器配置变化事件（可选，取决于发现策略是否支持）
    /// </summary>
    event EventHandler<McpServerChangedEventArgs>? ServerChanged;
}

/// <summary>
/// MCP 服务器配置变化事件参数
/// </summary>
public class McpServerChangedEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 McpServerChangedEventArgs
    /// </summary>
    /// <param name="changeType">变化类型</param>
    /// <param name="server">服务器配置</param>
    public McpServerChangedEventArgs(McpServerChangeType changeType, IMcpServerConfiguration server)
    {
        this.ChangeType = changeType;
        this.Server = server;
    }

    /// <summary>
    /// 获取变化类型
    /// </summary>
    public McpServerChangeType ChangeType { get; }

    /// <summary>
    /// 获取服务器配置
    /// </summary>
    public IMcpServerConfiguration Server { get; }
}

/// <summary>
/// MCP 服务器变化类型
/// </summary>
public enum McpServerChangeType
{
    /// <summary>
    /// 新增服务器
    /// </summary>
    Added,

    /// <summary>
    /// 更新服务器配置
    /// </summary>
    Updated,

    /// <summary>
    /// 移除服务器
    /// </summary>
    Removed
}
