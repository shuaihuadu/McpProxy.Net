using ModelContextProtocol.Client;
using System.Text.Json.Serialization;

namespace McpProxy.Models;

/// <summary>
/// 标准输入/输出服务器参数
/// </summary>
public sealed class StdioServerOptions : McpServerOptions
{
    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "stdio";

    /// <summary>
    /// 获取或设置运行服务器的命令
    /// </summary>
    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置传递给服务器的命令行参数列表
    /// </summary>
    [JsonPropertyName("args")]
    public IList<string>? Arguments { get; set; }

    /// <summary>
    /// 获取或设置传递给服务器的环境变量
    /// </summary>
    [JsonPropertyName("env")]
    public IDictionary<string, string?>? EnvironmentVariables { get; }

    /// <summary>
    /// 获取或设置工作目录
    /// </summary>
    [JsonPropertyName("cwd")]
    public string? WorkingDirectory { get; set; }

    public StdioClientTransportOptions ToStdioClientTransportOptions()
    {
        return new StdioClientTransportOptions
        {
            Name = this.Name,
            Command = this.Command,
            Arguments = this.Arguments,
            EnvironmentVariables = this.EnvironmentVariables,
            WorkingDirectory = this.WorkingDirectory,
            ShutdownTimeout = TimeSpan.FromSeconds(this.TimeoutSeconds),
            // TODO StandardErrorLines
        };
    }
}
