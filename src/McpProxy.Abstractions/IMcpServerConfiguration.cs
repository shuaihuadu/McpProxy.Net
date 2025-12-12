// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.Abstractions;

/// <summary>
/// MCP 服务器配置接口
/// 表示单个 MCP 服务器的配置信息
/// </summary>
public interface IMcpServerConfiguration
{
    /// <summary>
    /// 获取服务器唯一标识符
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 获取服务器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 获取启动命令
    /// </summary>
    string Command { get; }

    /// <summary>
    /// 获取命令行参数
    /// </summary>
    IReadOnlyList<string> Arguments { get; }

    /// <summary>
    /// 获取环境变量
    /// </summary>
    IReadOnlyDictionary<string, string> Environment { get; }

    /// <summary>
    /// 获取工作目录
    /// </summary>
    string? WorkingDirectory { get; }

    /// <summary>
    /// 获取是否启用
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// 获取服务器标签（用于分类和过滤）
    /// </summary>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// 获取元数据（扩展信息）
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
