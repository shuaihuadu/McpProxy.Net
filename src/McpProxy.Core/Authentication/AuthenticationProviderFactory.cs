namespace McpProxy;

/// <summary>
/// 认证提供者工厂
/// </summary>
public static class AuthenticationProviderFactory
{
    /// <summary>
    /// 创建认证提供者
    /// </summary>
    /// <param name="options">认证配置</param>
    /// <param name="loggerFactory">日志记录器</param>
    /// <param name="httpClientFactory">HTTP客户端（用于OAuth2）</param>
    /// <returns>认证提供者实例</returns>
    public static IAuthenticationProvider CreateAuthenticationProvider(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<AuthenticationOptions> options)
    {
        AuthenticationOptions authenticationOptions = options.Value;

        Verify.NotNull(authenticationOptions, nameof(authenticationOptions));

        return authenticationOptions.Type switch
        {
            AuthenticationType.Bearer => new BearerTokenAuthenticationProvider(loggerFactory, options),
            AuthenticationType.Basic => new BasicAuthenticationProvider(loggerFactory, options),
            AuthenticationType.OAuth2ClientCredentials => new OAuth2ClientCredentialsAuthenticationProvider(loggerFactory, options, httpClientFactory),
            _ => throw new ArgumentNullException(nameof(options))
        };
    }
}