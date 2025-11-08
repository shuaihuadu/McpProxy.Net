namespace McpProxy;

/// <summary>
/// OAuth2客户端凭证认证提供者
/// </summary>
public class OAuth2ClientCredentialsAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<OAuth2ClientCredentialsAuthenticationProvider> _logger;
    private readonly AuthenticationOptions _authenticationOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);

    /// <inheritdoc />
    public OAuth2ClientCredentialsAuthenticationProvider(ILoggerFactory loggerFactory, IOptions<AuthenticationOptions> authenticationOptions, IHttpClientFactory httpClientFactory)
    {
        this._logger = loggerFactory.CreateLogger<OAuth2ClientCredentialsAuthenticationProvider>();
        this._authenticationOptions = authenticationOptions.Value;
        this._httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken = default)
    {
        // 如果没有令牌或令牌即将在1分钟内过期，则需要刷新
        bool shouldRefresh = string.IsNullOrEmpty(this._authenticationOptions.AccessToken) ||
                            (this._authenticationOptions.TokenExpiry.HasValue &&
                             this._authenticationOptions.TokenExpiry.Value <= DateTime.UtcNow.AddMinutes(1));

        // 如果令牌不存在或已过期，则获取新令牌
        if (string.IsNullOrWhiteSpace(this._authenticationOptions.AccessToken) || shouldRefresh)
        {
            await this.RefreshTokenAsync().ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(this._authenticationOptions.AccessToken))
        {
            this._logger.LogWarning("Can not get validate access token");

            return default;
        }

        AuthenticationHeaderValue header = new("Bearer", this._authenticationOptions.AccessToken);

        return header;
    }

    /// <inheritdoc />
    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await this._refreshTokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            this._logger.LogDebug("Refresh OAuth2 access token");

            if (string.IsNullOrWhiteSpace(this._authenticationOptions.TokenUrl)
                || string.IsNullOrWhiteSpace(this._authenticationOptions.ClientId)
                || string.IsNullOrWhiteSpace(this._authenticationOptions.ClientSecret))
            {
                throw new InvalidOperationException($"Invalid oauth2 configuration, need provider the {nameof(this._authenticationOptions.TokenUrl)},{nameof(this._authenticationOptions.ClientId)} and {nameof(this._authenticationOptions.ClientSecret)}");
            }

            Dictionary<string, string> tokenRequest = new()
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = this._authenticationOptions.ClientId,
                ["client_secret"] = this._authenticationOptions.ClientSecret
            };

            if (this._authenticationOptions.Scopes is not null && this._authenticationOptions.Scopes.Length > 0)
            {
                tokenRequest["scope"] = string.Join(" ", this._authenticationOptions.Scopes);
            }

            using (HttpClient client = this._httpClientFactory.CreateClient())
            {
                using HttpRequestMessage request = new(HttpMethod.Post, this._authenticationOptions.TokenUrl)
                {
                    Content = new FormUrlEncodedContent(tokenRequest)
                };

                HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    throw new HttpRequestException($"Get oauth2 token failed: {response.StatusCode} - {errorContent}");
                }

                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await this.ParseTokenResponseAsync(responseContent).ConfigureAwait(false);

                this._logger.LogInformation("OAuth2访问令牌刷新成功");
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Refresh oauth2 token occurred an error");
            throw;
        }
        finally
        {
            this._refreshTokenLock.Release();
        }
    }

    /// <summary>
    /// 解析令牌响应
    /// </summary>
    /// <param name="responseContent">响应内容</param>
    /// <returns>表示异步操作的任务</returns>
    private Task ParseTokenResponseAsync(string responseContent)
    {
        // TODO 实际应该使用System.Text.Json解析JSON
        // 目前假设响应包含 access_token 和 expires_in
        try
        {
            // TODO 使用JsonSerializer反序列化处理
            JsonDocument jsonDocument = JsonDocument.Parse(responseContent);

            JsonElement root = jsonDocument.RootElement;

            if (root.TryGetProperty("access_token", out JsonElement accessTokenElement))
            {
                this._authenticationOptions.AccessToken = accessTokenElement.GetString();
            }

            if (root.TryGetProperty("expires_in", out JsonElement expiresInElement) && expiresInElement.TryGetInt32(out int expiresIn))
            {
                this._authenticationOptions.TokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 提前1分钟过期，以便Refresh
            }

            this._logger.LogDebug("OAuth2 token parse completed, The token will expired at: {TokenExpiry}", this._authenticationOptions.TokenExpiry);
        }
        catch (Exception ex)
        {
            string message = "Parse oauth2 token response occurred an error";

            this._logger.LogError(exception: ex, message: message);

            throw new InvalidOperationException(message, ex);
        }

        return Task.CompletedTask;
    }
}
