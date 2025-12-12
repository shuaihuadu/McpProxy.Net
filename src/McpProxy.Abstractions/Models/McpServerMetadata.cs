namespace McpProxy;

/// <summary>
/// 表示MCP服务器的元数据信息
/// </summary>
/// <param name="id">分配给服务器的唯一标识符。如果未指定，默认为空字符串</param>
/// <param name="name">服务器的显示名称。如果未指定，默认为空字符串</param>
/// <param name="description">服务器用途或功能的描述。如果未指定，默认为空字符串</param>
public sealed class McpServerMetadata(string id = "", string name = "", string description = "")
{
    /// <summary>
    /// 获取或设置服务器的唯一标识符
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// 获取或设置服务器的显示名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 获取或设置服务器的用户友好标题，用于显示目的
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置服务器用途或功能的描述
    /// </summary>
    public string Description { get; set; } = description;
}
