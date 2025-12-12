// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// MCP运行时的实现类
/// 提供日志记录和配置支持，并负责处理工具列表和工具调用、提示词管理、资源访问等请求
/// </summary>
public class McpRuntime : IMcpRuntime
{
    /// <summary>
    /// 获取MCP代理服务实例
    /// </summary>
    private readonly IMcpProxyService _proxyService;

    /// <summary>
    /// 获取日志记录器实例
    /// </summary>
    private readonly ILogger<McpRuntime> _logger;

    /// <summary>
    /// 初始化<see cref="McpRuntime"/>类的新实例
    /// </summary>
    /// <param name="proxyService">MCP代理服务实例</param>
    /// <param name="logger">日志记录器实例</param>
    /// <exception cref="ArgumentNullException">当proxyService或logger为null时抛出</exception>
    public McpRuntime(IMcpProxyService proxyService, ILogger<McpRuntime> logger)
    {
        this._proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 记录运行时初始化信息
        this._logger.LogInformation("McpRuntime initialized with proxy service of type {ProxyServiceType}.", this._proxyService.GetType().Name);
    }

    // ========== Tools Handlers ==========

    /// <inheritdoc />
    public async ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams>? request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 如果有完整的请求上下文，直接使用
            if (request?.Params != null)
            {
                ListToolsResult result = await this._proxyService.ListToolsAsync(request.Params, cancellationToken).ConfigureAwait(false);
                return result;
            }

            // 否则使用简化版本
            ListToolsResult fallbackResult = await this._proxyService.ListToolsAsync(null, null, cancellationToken).ConfigureAwait(false);
            return fallbackResult;
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

            // 调用代理服务执行工具
            CallToolResult result = await this._proxyService.CallToolAsync(callToolRequestParams, cancellationToken).ConfigureAwait(false);

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

    // ========== Prompts Handlers ==========

    /// <inheritdoc />
    public async ValueTask<ListPromptsResult> ListPromptsHandler(RequestContext<ListPromptsRequestParams>? request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 如果有完整的请求上下文，直接使用
            if (request?.Params != null)
            {
                ListPromptsResult result = await this._proxyService.ListPromptsAsync(request.Params, cancellationToken).ConfigureAwait(false);
                return result;
            }

            // 否则使用简化版本
            ListPromptsResult fallbackResult = await this._proxyService.ListPromptsAsync(null, null, cancellationToken).ConfigureAwait(false);
            return fallbackResult;
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Prompts功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while listing prompts.");
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<GetPromptResult> GetPromptHandler(RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken = default)
    {
        // 验证请求参数
        if (request?.Params == null)
        {
            this._logger.LogWarning("Cannot get prompt with null parameters.");
            throw new ArgumentNullException(nameof(request), "Cannot get prompt with null parameters.");
        }

        try
        {
            // 记录获取提示词请求
            this._logger.LogInformation("Getting prompt '{PromptName}'.", request.Params.Name);

            // 调用代理服务获取提示词
            GetPromptResult result = await this._proxyService.GetPromptAsync(request, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Prompt '{PromptName}' retrieved successfully.", request.Params.Name);

            return result;
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Prompts功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while getting prompt '{PromptName}'.", request.Params?.Name);
            throw;
        }
    }

    // ========== Resources Handlers ==========

    /// <inheritdoc />
    public async ValueTask<ListResourcesResult> ListResourcesHandler(RequestContext<ListResourcesRequestParams>? request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 如果有完整的请求上下文，直接使用
            if (request?.Params != null)
            {
                ListResourcesResult result = await this._proxyService.ListResourcesAsync(request.Params, cancellationToken).ConfigureAwait(false);
                return result;
            }

            // 否则使用简化版本
            ListResourcesResult fallbackResult = await this._proxyService.ListResourcesAsync(null, null, cancellationToken).ConfigureAwait(false);
            return fallbackResult;
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Resources功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while listing resources.");
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<ReadResourceResult> ReadResourceHandler(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken = default)
    {
        // 验证请求参数
        if (request?.Params == null)
        {
            this._logger.LogWarning("Cannot read resource with null parameters.");
            throw new ArgumentNullException(nameof(request), "Cannot read resource with null parameters.");
        }

        try
        {
            // 记录读取资源请求
            this._logger.LogInformation("Reading resource '{ResourceUri}'.", request.Params.Uri);

            // 调用代理服务读取资源
            ReadResourceResult result = await this._proxyService.ReadResourceAsync(request, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Resource '{ResourceUri}' read successfully.", request.Params.Uri);

            return result;
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Resources功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while reading resource '{ResourceUri}'.", request.Params?.Uri);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask SubscribeResourceHandler(RequestContext<SubscribeRequestParams> request, CancellationToken cancellationToken = default)
    {
        // 验证请求参数
        if (request?.Params == null)
        {
            this._logger.LogWarning("Cannot subscribe resource with null parameters.");
            throw new ArgumentNullException(nameof(request), "Cannot subscribe resource with null parameters.");
        }

        try
        {
            // 记录订阅资源请求
            this._logger.LogInformation("Subscribing to resource '{ResourceUri}'.", request.Params.Uri);

            // 调用代理服务订阅资源
            await this._proxyService.SubscribeResourceAsync(request, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Successfully subscribed to resource '{ResourceUri}'.", request.Params.Uri);
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Resources订阅功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while subscribing to resource '{ResourceUri}'.", request.Params?.Uri);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask UnsubscribeResourceHandler(RequestContext<UnsubscribeRequestParams> request, CancellationToken cancellationToken = default)
    {
        // 验证请求参数
        if (request?.Params == null)
        {
            this._logger.LogWarning("Cannot unsubscribe resource with null parameters.");
            throw new ArgumentNullException(nameof(request), "Cannot unsubscribe resource with null parameters.");
        }

        try
        {
            // 记录取消订阅资源请求
            this._logger.LogInformation("Unsubscribing from resource '{ResourceUri}'.", request.Params.Uri);

            // 调用代理服务取消订阅资源
            await this._proxyService.UnsubscribeResourceAsync(request, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Successfully unsubscribed from resource '{ResourceUri}'.", request.Params.Uri);
        }
        catch (NotImplementedException)
        {
            this._logger.LogWarning("Resources取消订阅功能尚未实现");
            throw;
        }
        catch (Exception ex)
        {
            // 记录错误并重新抛出异常
            this._logger.LogError(ex, "Error occurred while unsubscribing from resource '{ResourceUri}'.", request.Params?.Uri);
            throw;
        }
    }

    /// <summary>
    /// 释放代理服务并释放相关资源
    /// </summary>
    /// <returns>表示异步释放操作的任务</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            // 释放代理服务资源
            await this._proxyService.DisposeAsync().ConfigureAwait(false);

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