namespace McpProxy;

/// <summary>
/// MCPProxy 的 HttpClientProvider 实现
/// </summary>
public class HttpClientProvider : IHttpClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HttpClientProvider> _logger;
    private readonly IOptions<AuthenticationOptions> _options;
    private readonly AuthenticationOptions _authenticationOptions;

    /// <inheritdoc />
    public HttpClientProvider(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<AuthenticationOptions> options)
    {
        this._loggerFactory = loggerFactory;
        this._logger = loggerFactory.CreateLogger<HttpClientProvider>();
        this._httpClientFactory = httpClientFactory;
        this._options = options;
        this._authenticationOptions = options.Value;
    }

    /// <inheritdoc />
    public async Task<HttpClient> GetHttpClientAsync(string clientName, HttpClientOptions httpClientOptions, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = this._httpClientFactory.CreateClient(clientName);

        if (httpClientOptions.TimeoutSeconds > 0)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(httpClientOptions.TimeoutSeconds);
        }

        if (httpClientOptions.Headers is not null)
        {
            foreach (KeyValuePair<string, string?> header in httpClientOptions.Headers)
            {
                if (!httpClient.DefaultRequestHeaders.Contains(header.Key))
                {
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        if (this._authenticationOptions is not null && this._authenticationOptions.Type != AuthenticationType.None)
        {
            await this.ConfigureAuthenticationAsync(httpClient, cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogDebug("Create HTTP Client '{ClientName}'，Authentication Type: {AuthenticationType}", clientName, _authenticationOptions?.Type.ToString() ?? AuthenticationType.None.ToString());

        return httpClient;
    }

    private async Task ConfigureAuthenticationAsync(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        try
        {
            IAuthenticationProvider authenticationProvider = AuthenticationProviderFactory.CreateAuthenticationProvider(this._loggerFactory, this._httpClientFactory, this._options);

            AuthenticationHeaderValue? authenticationHeaderValue = await authenticationProvider.GetAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);

            if (authenticationHeaderValue != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;

                this._logger.LogDebug("Set authentication header: {Scheme} {Parameter} ...", authenticationHeaderValue.Scheme, authenticationHeaderValue.Parameter?[..Math.Min(10, authenticationHeaderValue.Parameter.Length)]);
            }

            if (this._authenticationOptions.Type == AuthenticationType.ApiKey)
            {
                this.ConfigureApiKey(httpClient);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Configure authentication occur an error");
            throw;
        }
    }

    private void ConfigureApiKey(HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(this._authenticationOptions.ApiKey)
            || string.IsNullOrWhiteSpace(this._authenticationOptions.ApiKeyName)
            || this._authenticationOptions.ApiKeyLocation is null)
        {
            this._logger.LogWarning("Invalid api key configuration");

            return;
        }

        switch (this._authenticationOptions.ApiKeyLocation)
        {
            case ApiKeyLocation.Header:
                if (!httpClient.DefaultRequestHeaders.Contains(this._authenticationOptions.ApiKeyName))
                {
                    httpClient.DefaultRequestHeaders.Add(this._authenticationOptions.ApiKeyName, this._authenticationOptions.ApiKey);
                    this._logger.LogDebug("Set api key header : {HeaderName}", this._authenticationOptions.ApiKeyName);
                }
                break;
            case ApiKeyLocation.Query:
                // 查询参数需要在每个请求中单独处理，此处无需设置
                break;
        }
    }
}
