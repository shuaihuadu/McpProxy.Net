namespace McpProxy;

/// <summary>
/// Bearer Token认证提供者
/// </summary>
public class BearerTokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<BearerTokenAuthenticationProvider> _logger;
    private readonly AuthenticationOptions _authenticationOptions;

    /// <inheritdoc />
    public BearerTokenAuthenticationProvider(ILoggerFactory loggerFactory, IOptions<AuthenticationOptions> authenticationOptions)
    {
        this._logger = loggerFactory.CreateLogger<BearerTokenAuthenticationProvider>();
        this._authenticationOptions = authenticationOptions.Value;
    }

    /// <inheritdoc />
    public Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this._authenticationOptions.BearerToken))
        {
            return Task.FromResult<AuthenticationHeaderValue?>(default);
        }

        AuthenticationHeaderValue authenticationHeader = new("Bearer", this._authenticationOptions.BearerToken);

        return Task.FromResult(authenticationHeader)!;
    }

    /// <inheritdoc />
    public Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
