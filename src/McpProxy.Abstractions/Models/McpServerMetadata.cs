namespace McpProxy;

/// <summary>
/// Represents metadata information for a mcp server.
/// </summary>
/// <param name="id">The unique identifier assigned to the server. If not specified, defaults to an empty string.</param>
/// <param name="name">The display name of the server. If not specified, defaults to an empty string.</param>
/// <param name="description">A description of the server's purpose or capabilities. If not specified, defaults to an empty string.</param>
public sealed class McpServerMetadata(string id = "", string name = "", string description = "")
{
    /// <summary>
    /// Gets or sets the unique identifier for the server.
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// Gets or sets the display name of the server.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the user-friendly title of the server for display purposes.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a description of the server's purpose or capabilities.
    /// </summary>
    public string Description { get; set; } = description;
}
