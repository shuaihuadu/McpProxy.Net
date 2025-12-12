using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// 连接到基于Stdio的MCP服务器的配置
/// </summary>
public sealed class StdioClientOptions
{
    /// <summary>
    /// 获取或设置要执行的命令（例如："npx"、"uvx"、"python"）
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
}
