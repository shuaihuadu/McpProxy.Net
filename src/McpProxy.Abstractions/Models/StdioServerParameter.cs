namespace McpProxy.Models;

/// <summary>
/// 标准输入/输出服务器参数
/// </summary>
public sealed class StdioServerParameter
{

    const int TimeoutDefault = 60;

    /// <summary>
    /// 命令可执行文件或脚本名称
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// 命令行参数列表
    /// </summary>
    public IReadOnlyList<string>? Args { get; }

    /// <summary>
    /// 环境变量字典（键->值）
    /// </summary>
    public IReadOnlyDictionary<string, string?>? Env { get; }

    /// <summary>
    /// 工作目录（可空）
    /// </summary>
    public string? Cwd { get; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = TimeoutDefault;

    /// <summary>
    /// 构造函数，初始化所有属性
    /// </summary>
    /// <param name="command">命令</param>
    /// <param name="args">参数列表</param>
    /// <param name="env">环境变量</param>
    /// <param name="cwd">工作目录</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    public StdioServerParameter(string command, IReadOnlyList<string>? args, IReadOnlyDictionary<string, string?>? env, string? cwd, int timeoutSeconds = TimeoutDefault)
    {
        Verify.NotNullOrWhiteSpace(command, nameof(command));

        this.Command = command;
        this.Args = args;
        this.Env = env;
        this.Cwd = cwd;
        this.TimeoutSeconds = timeoutSeconds;
    }
}
