namespace McpProxy;

/// <summary>
/// Defines the supported transport mechanisms for the Azure MCP server.
/// </summary>
public static class TransportTypes
{
    /// <summary>
    /// Standard Input/Output transport mechanism.
    /// </summary>
    public const string StdIo = "stdio";

    /// <summary>
    /// MCP's bespoke transport called Streamable HTTP.
    /// </summary>
    public const string Http = "http";
}
