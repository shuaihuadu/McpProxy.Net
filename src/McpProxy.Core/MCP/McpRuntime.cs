namespace McpProxy;

/// <summary>
/// MCP运行时的实现类
/// 提供日志记录和配置支持，并负责处理工具列表和工具调用请求
/// </summary>
public class McpRuntime : IMcpRuntime
{
    /// <summary>
    /// 获取MCP工具处理器实例
    /// </summary>
    private readonly IMcpToolsHandler _toolsHandler;

    /// <summary>
    /// 获取日志记录器实例
    /// </summary>
    private readonly ILogger<McpRuntime> _logger;

    /// <summary>
    /// 初始化<see cref="McpRuntime"/>类的新实例
    /// </summary>
    /// <param name="toolsHandler">MCP工具处理器实例</param>
    /// <param name="logger">日志记录器实例</param>
    /// <exception cref="ArgumentNullException">当toolsHandler或logger为null时抛出</exception>
    public McpRuntime(IMcpToolsHandler toolsHandler, ILogger<McpRuntime> logger)
    {
        this._toolsHandler = toolsHandler ?? throw new ArgumentNullException(nameof(toolsHandler));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 记录运行时初始化信息
        this._logger.LogInformation("McpRuntime initialized with tool handler of type {ToolHandlerType}.", this._toolsHandler.GetType().Name);
    }

    /// <inheritdoc />
    public async ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams>? request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 调用工具处理器列出所有工具
            ListToolsResult result = await this._toolsHandler.ListToolsAsync(default, cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while listing tools.");
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<CallToolResult> CallToolHandler(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        // 验证请求参数
        if (request?.Params == null)
        {
            TextContentBlock content = new()
            {
                Text = "Cannot call tools with null parameters.",
            };

            this._logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        // 调用重载方法处理工具调用
        return await this.CallToolHandler(request.Params, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CallToolResult> CallToolHandler(CallToolRequestParams callToolRequestParams, CancellationToken cancellationToken = default)
    {
        // 验证参数不为null
        if (callToolRequestParams == null)
        {
            TextContentBlock content = new()
            {
                Text = "Cannot call tools with null parameters.",
            };

            this._logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        try
        {
            // 记录工具调用请求
            this._logger.LogInformation("Calling tool '{ToolName}' with parameters.", callToolRequestParams.Name);

            // 调用工具处理器执行工具
            CallToolResult result = await this._toolsHandler.CallToolsAsync(callToolRequestParams, cancellationToken).ConfigureAwait(false);

            // 记录调用结果
            if (result.IsError == true)
            {
                this._logger.LogWarning("Tool '{ToolName}' execution failed.", callToolRequestParams.Name);
            }
            else
            {
                this._logger.LogInformation("Tool '{ToolName}' executed successfully.", callToolRequestParams.Name);
            }

            return result;
        }
        catch (InvalidOperationException ex)
        {
            // 处理操作无效异常，返回错误结果
            this._logger.LogError(ex, "Invalid operation when calling tool '{ToolName}'.", callToolRequestParams.Name);

            return new CallToolResult
            {
                Content = [new TextContentBlock
                {
                    Text = ex.Message,
                }],
                IsError = true,
            };
        }
        catch (Exception ex)
        {
            // 记录未预期的异常并重新抛出
            this._logger.LogError(ex, "Unexpected error occurred while calling tool '{ToolName}'.", callToolRequestParams.Name);
            throw;
        }
    }

    /// <summary>
    /// 释放工具处理器并释放相关资源
    /// </summary>
    /// <returns>表示异步释放操作的任务</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            // 释放工具处理器资源
            await this._toolsHandler.DisposeAsync().ConfigureAwait(false);

            this._logger.LogInformation("McpRuntime disposed successfully.");
        }
        catch (Exception ex)
        {
            // 记录释放过程中的错误
            this._logger.LogError(ex, "Error occurred while disposing McpRuntime.");
            throw;
        }
    }
}