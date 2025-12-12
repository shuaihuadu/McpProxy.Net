// Copyright (c) IdeaTech. All rights reserved.

using System.Net.Http.Headers;
using System.Text.Json;

namespace McpProxy.Core.Authentication;

/// <summary>
/// OAuth2客户端凭据流（Client Credentials Flow）的DelegatingHandler实现
/// 用于自动获取和刷新访问令牌
/// </summary>
public sealed class OAuth2ClientCredentialsHandler : DelegatingHandler
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenUrl;
    private readonly string? _scope;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiration = DateTimeOffset.MinValue;

    /// <summary>
    /// 初始化OAuth2ClientCredentialsHandler的新实例
    /// </summary>
    /// <param name="clientId">OAuth2客户端ID</param>
    /// <param name="clientSecret">OAuth2客户端密钥</param>
    /// <param name="tokenUrl">OAuth2令牌端点URL</param>
    /// <param name="scope">OAuth2请求的作用域（可选）</param>
    public OAuth2ClientCredentialsHandler(
        string clientId,
        string clientSecret,
        string tokenUrl,
        string? scope = null)
    {
        this._clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        this._clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        this._tokenUrl = tokenUrl ?? throw new ArgumentNullException(nameof(tokenUrl));
        this._scope = scope;
    }

    /// <summary>
    /// 发送HTTP请求，在需要时自动获取或刷新访问令牌
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP响应消息</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 确保有有效的访问令牌
        await this.EnsureValidTokenAsync(cancellationToken).ConfigureAwait(false);

        // 添加授权头
        if (!string.IsNullOrEmpty(this._accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this._accessToken);
        }

        // 发送请求
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 确保当前有有效的访问令牌，如果没有或已过期则获取新令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        // 如果令牌仍然有效，直接返回
        if (!string.IsNullOrEmpty(this._accessToken) && DateTimeOffset.UtcNow < this._tokenExpiration)
        {
            return;
        }

        // 使用锁避免并发获取令牌
        await this._lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 双重检查：可能在等待锁期间其他线程已经获取了令牌
            if (!string.IsNullOrEmpty(this._accessToken) && DateTimeOffset.UtcNow < this._tokenExpiration)
            {
                return;
            }

            // 获取新的访问令牌
            await this.FetchAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._lock.Release();
        }
    }

    /// <summary>
    /// 从OAuth2令牌端点获取访问令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task FetchAccessTokenAsync(CancellationToken cancellationToken)
    {
        // 准备令牌请求
        Dictionary<string, string> requestData = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = this._clientId,
            ["client_secret"] = this._clientSecret
        };

        if (!string.IsNullOrEmpty(this._scope))
        {
            requestData["scope"] = this._scope;
        }

        FormUrlEncodedContent content = new(requestData);

        // 创建临时HttpClient用于获取令牌（不使用本Handler避免递归）
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(30);

        // 发送令牌请求
        HttpResponseMessage response = await client.PostAsync(
            this._tokenUrl,
            content,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        // 解析响应
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(responseBody);
        JsonElement root = doc.RootElement;

        // 提取访问令牌
        if (!root.TryGetProperty("access_token", out JsonElement accessTokenElement))
        {
            throw new InvalidOperationException("OAuth2 token response missing 'access_token' field");
        }

        this._accessToken = accessTokenElement.GetString();

        // 提取过期时间（默认3600秒）
        int expiresIn = 3600;
        if (root.TryGetProperty("expires_in", out JsonElement expiresInElement))
        {
            expiresIn = expiresInElement.GetInt32();
        }

        // 设置过期时间（提前60秒刷新以避免边界情况）
        this._tokenExpiration = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._lock.Dispose();
        }

        base.Dispose(disposing);
    }
}
