namespace McpProxy;

/// <summary>
/// Basic认证提供者
/// </summary>
public class BasicAuthenticationProvider : IAuthenticationProvider
{
    private readonly ILogger<BasicAuthenticationProvider> _logger;
    private readonly AuthenticationOptions _authenticationOptions;

    /// <inheritdoc />
    public BasicAuthenticationProvider(ILoggerFactory loggerFactory, IOptions<AuthenticationOptions> authenticationOptions)
    {
        this._logger = loggerFactory.CreateLogger<BasicAuthenticationProvider>();
        this._authenticationOptions = authenticationOptions.Value;
    }

    /// <inheritdoc />
    public Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(this._authenticationOptions.Username) || string.IsNullOrEmpty(this._authenticationOptions.Password))
        {
            return Task.FromResult<AuthenticationHeaderValue?>(null);
        }

        string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{this._authenticationOptions.Username}:{this._authenticationOptions.Password}"));

        AuthenticationHeaderValue header = new("Basic", credentials);

        return Task.FromResult<AuthenticationHeaderValue?>(header);
    }

    /// <inheritdoc />
    public Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
