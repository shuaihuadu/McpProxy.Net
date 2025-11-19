namespace McpProxy;

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
