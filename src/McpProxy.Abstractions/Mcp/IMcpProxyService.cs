// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// 定义 stdio 到 Streamable HTTP 的完整 MCP 代理服务接口
/// 支持 Tools、Prompts 和 Resources 的代理功能
/// </summary>
public interface IMcpProxyService : IAsyncDisposable
{
    // ========== Tools 操作 ==========

    /// <summary>
    /// 列出所有可用工具（使用请求上下文）
    /// </summary>
    /// <param name="request">列出工具的请求上下文，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具列表结果，包含工具定义和可选的下一页游标</returns>
    ValueTask<ListToolsResult> ListToolsAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用工具（使用参数对象）
    /// </summary>
    /// <param name="listToolsRequestParams">列出工具的参数对象，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具列表结果，包含工具定义和可选的下一页游标</returns>
    ValueTask<ListToolsResult> ListToolsAsync(
        ListToolsRequestParams listToolsRequestParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用工具（简化版本）
    /// </summary>
    /// <param name="mcpServerName">MCP服务器名称。如果为null或空，列出所有服务器的工具</param>
    /// <param name="cursor">分页游标，用于获取下一页结果</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具列表结果，包含工具定义和可选的下一页游标</returns>
    ValueTask<ListToolsResult> ListToolsAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 调用特定工具（使用请求上下文）
    /// </summary>
    /// <param name="request">调用工具的请求上下文，包含工具名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具调用结果，包含执行结果或错误信息</returns>
    ValueTask<CallToolResult> CallToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 调用特定工具（使用参数对象）
    /// </summary>
    /// <param name="callToolRequestParams">调用工具的参数对象，包含工具名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具调用结果，包含执行结果或错误信息</returns>
    ValueTask<CallToolResult> CallToolAsync(
        CallToolRequestParams callToolRequestParams,
        CancellationToken cancellationToken = default);

    // ========== Prompts 操作 ==========

    /// <summary>
    /// 列出所有可用提示词模板（使用请求上下文）
    /// </summary>
    /// <param name="request">列出提示词的请求上下文，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>提示词列表结果，包含提示词定义和可选的下一页游标</returns>
    ValueTask<ListPromptsResult> ListPromptsAsync(
        RequestContext<ListPromptsRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用提示词模板（使用参数对象）
    /// </summary>
    /// <param name="listPromptsRequestParams">列出提示词的参数对象，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>提示词列表结果，包含提示词定义和可选的下一页游标</returns>
    ValueTask<ListPromptsResult> ListPromptsAsync(
        ListPromptsRequestParams listPromptsRequestParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用提示词模板（简化版本）
    /// </summary>
    /// <param name="mcpServerName">MCP服务器名称。如果为null或空，列出所有服务器的提示词</param>
    /// <param name="cursor">分页游标，用于获取下一页结果</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>提示词列表结果，包含提示词定义和可选的下一页游标</returns>
    ValueTask<ListPromptsResult> ListPromptsAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取特定提示词的内容（使用请求上下文）
    /// </summary>
    /// <param name="request">获取提示词的请求上下文，包含提示词名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>提示词内容结果，包含提示词消息列表</returns>
    ValueTask<GetPromptResult> GetPromptAsync(
        RequestContext<GetPromptRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取特定提示词的内容（使用参数对象）
    /// </summary>
    /// <param name="getPromptRequestParams">获取提示词的参数对象，包含提示词名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>提示词内容结果，包含提示词消息列表</returns>
    ValueTask<GetPromptResult> GetPromptAsync(
        GetPromptRequestParams getPromptRequestParams,
        CancellationToken cancellationToken = default);

    // ========== Resources 操作 ==========

    /// <summary>
    /// 列出所有可用资源（使用请求上下文）
    /// </summary>
    /// <param name="request">列出资源的请求上下文，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>资源列表结果，包含资源定义和可选的下一页游标</returns>
    ValueTask<ListResourcesResult> ListResourcesAsync(
        RequestContext<ListResourcesRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用资源（使用参数对象）
    /// </summary>
    /// <param name="listResourcesRequestParams">列出资源的参数对象，包含分页游标等参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>资源列表结果，包含资源定义和可选的下一页游标</returns>
    ValueTask<ListResourcesResult> ListResourcesAsync(
        ListResourcesRequestParams listResourcesRequestParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用资源（简化版本）
    /// </summary>
    /// <param name="mcpServerName">MCP服务器名称。如果为null或空，列出所有服务器的资源</param>
    /// <param name="cursor">分页游标，用于获取下一页结果</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>资源列表结果，包含资源定义和可选的下一页游标</returns>
    ValueTask<ListResourcesResult> ListResourcesAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取特定资源的内容（使用请求上下文）
    /// </summary>
    /// <param name="request">读取资源的请求上下文，包含资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>资源内容结果，包含资源内容块列表</returns>
    ValueTask<ReadResourceResult> ReadResourceAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取特定资源的内容（使用参数对象）
    /// </summary>
    /// <param name="readResourceRequestParams">读取资源的参数对象，包含资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>资源内容结果，包含资源内容块列表</returns>
    ValueTask<ReadResourceResult> ReadResourceAsync(
        ReadResourceRequestParams readResourceRequestParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅资源更新通知（使用请求上下文）
    /// </summary>
    /// <param name="request">订阅请求上下文，包含要订阅的资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask SubscribeResourceAsync(
        RequestContext<SubscribeRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅资源更新通知（使用参数对象）
    /// </summary>
    /// <param name="subscribeRequestParams">订阅请求参数，包含要订阅的资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask SubscribeResourceAsync(
        SubscribeRequestParams subscribeRequestParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅资源更新通知（使用请求上下文）
    /// </summary>
    /// <param name="request">取消订阅请求上下文，包含要取消订阅的资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask UnsubscribeResourceAsync(
        RequestContext<UnsubscribeRequestParams> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅资源更新通知（使用参数对象）
    /// </summary>
    /// <param name="unsubscribeRequestParams">取消订阅请求参数，包含要取消订阅的资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask UnsubscribeResourceAsync(
        UnsubscribeRequestParams unsubscribeRequestParams,
        CancellationToken cancellationToken = default);
}
