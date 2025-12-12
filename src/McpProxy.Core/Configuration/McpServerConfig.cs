using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// MCP 服务器配置
/// 实现 <see cref="McpProxy.Abstractions.IMcpServerConfiguration"/> 接口
/// </summary>
public sealed class McpServerConfig : McpProxy.Abstractions.IMcpServerConfiguration
{
    /// <summary>
    /// 获取或设置服务器的唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 获取或设置服务器的唯一名称（多服务器时用作命名空间前缀）
    /// </summary>
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_-]*$", ErrorMessage = "Server name must start with a letter and contain only alphanumeric characters, hyphens, and underscores")]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置要执行的命令（例如："npx"、"python"）
    /// </summary>
    [Required(ErrorMessage = "Command is required")]
    public required string Command { get; set; }

    /// <summary>
    /// 获取或设置传递给命令的参数列表
    /// </summary>
    public List<string>? Arguments { get; set; }

    /// <summary>
    /// 获取或设置进程的环境变量
    /// </summary>
    public Dictionary<string, string>? Environment { get; set; }

    /// <summary>
    /// 获取或设置进程的工作目录
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// 获取或设置服务器是否启用（默认值：true）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置服务器标签（用于分类和过滤）
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 获取或设置元数据（扩展信息）
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    // 显式接口实现 - 将可空集合转换为只读集合
    string McpProxy.Abstractions.IMcpServerConfiguration.Name => 
        this.Name ?? this.Id; // 如果Name为空，使用Id

    IReadOnlyList<string> McpProxy.Abstractions.IMcpServerConfiguration.Arguments =>
        (IReadOnlyList<string>?)this.Arguments?.AsReadOnly() ?? Array.Empty<string>();

    IReadOnlyDictionary<string, string> McpProxy.Abstractions.IMcpServerConfiguration.Environment =>
        this.Environment as IReadOnlyDictionary<string, string> ?? new Dictionary<string, string>();

    IReadOnlyList<string> McpProxy.Abstractions.IMcpServerConfiguration.Tags =>
        (IReadOnlyList<string>?)this.Tags?.AsReadOnly() ?? Array.Empty<string>();

    IReadOnlyDictionary<string, object> McpProxy.Abstractions.IMcpServerConfiguration.Metadata =>
        this.Metadata as IReadOnlyDictionary<string, object> ?? new Dictionary<string, object>();
}
