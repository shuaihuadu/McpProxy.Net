using Microsoft.Extensions.DependencyInjection;

namespace McpProxy;

public class ServerToolsHandler : BaseMcpToolsHandler
{
    private readonly Dictionary<string, List<Tool>> _cachedToolLists = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider _services;

    public ServerToolsHandler(IServiceProvider services, ILogger<ServerToolsHandler> logger) : base(services, logger)
    {
        this._services = services;
    }

    public override async ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken = default)
    {
        IEnumerable<IMcpServerProvider> servers = this._services.GetServices<IMcpServerProvider>();

        ListToolsResult listToolsResult = new()
        {
            Tools = [],
        };

        foreach (IMcpServerProvider server in servers)
        {
            McpServerMetadata metadata = server.CreateMetadata().FirstOrDefault();

            Tool tool = new()
            {
                Name = metadata.Name,
                Description = metadata.Description,
            };

            // TODO Tool Annotations

            listToolsResult.Tools.Add(tool);
        }

        return listToolsResult;
    }

    public override async ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Params?.Name))
        {
            throw new ArgumentNullException(nameof(request.Params.Name), "Tool name cannot be null or empty.");
        }

        string tool = request.Params.Name;

        IReadOnlyDictionary<string, JsonElement>? args = request.Params.Arguments;

        string? command = null;

        if (args is not null)
        {
            if (args.TryGetValue("command", out var commendElement) && commendElement.ValueKind == JsonValueKind.String)
            {
                command = commendElement.GetString();
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(tool) && !string.IsNullOrEmpty(command))
            {
                var toolParams = GetParametersDictionary(request);
                return await InvokeChildToolAsync(request, tool, command, toolParams, cancellationToken);
            }
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Key not found while calling tool: {Tool}", tool);

            return new CallToolResult
            {
                Content =
                [
                    new TextContentBlock {
                        Text = $"""
                            The tool '{tool}.{command}' was not found or does not support the specified command.
                            Please ensure the tool name and command are correct.
                            If you want to learn about available tools, run again with the "learn=true" argument.
                        """
                    }
                ],
                IsError = true
            };
        }

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock {
                Text = """
                    The "command" parameters are required when not learning
                    Run again with the "learn" argument to get a list of available tools and their parameters.
                    To learn about a specific tool, use the "tool" argument with the name of the tool.
                    """
                }
            ]
        };
    }

    private async Task<CallToolResult> InvokeChildToolAsync(RequestContext<CallToolRequestParams> request, string tool, string command, Dictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        if (request.Params is null)
        {
            var content = new TextContentBlock
            {
                Text = "Cannot call tools with null parameters.",
            };

            this._logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }


        IEnumerable<IMcpServerProvider> servers = this._services.GetServices<IMcpServerProvider>();

        McpClient client;

        try
        {
            McpClientOptions clientOptions = CreateClientOptions(request.Server);

            IMcpServerProvider mcpServerProvider = await this.FindServerProviderAsync(tool, cancellationToken);

            client = await mcpServerProvider.GetOrCreateClientAsync(tool, clientOptions, cancellationToken);

            if (client is null)
            {
                this._logger.LogError("Failed to get provider client for tool: {Tool}", tool);

                return default;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while getting provider client for tool: {Tool}", tool);

            throw;
        }

        try
        {
            //await NotifyProgressAsync(request, $"Calling {tool} {command}...", cancellationToken);

            CallToolResult callToolResult = await client.CallToolAsync(command, parameters, cancellationToken: cancellationToken);

            if (callToolResult.IsError is true)
            {
                _logger.LogWarning("Tool {Tool} command {Command} returned an error.", tool, command);
            }

            foreach (var content in callToolResult.Content)
            {
                TextContentBlock? textContent = content as TextContentBlock;

                if (textContent == null || string.IsNullOrWhiteSpace(textContent.Text))
                {
                    continue;
                }

                if (textContent.Text.Contains("Missing required options", StringComparison.OrdinalIgnoreCase))
                {
                    string childToolSpecJson = await GetChildToolJsonAsync(request, tool, command, cancellationToken);

                    _logger.LogWarning("Tool {Tool} command {Command} requires additional parameters.", tool, command);

                    CallToolResult finalResult = new CallToolResult
                    {
                        Content =
                        [
                            new TextContentBlock{
                                    Text = $"""
                                        The '{command}' command is missing required parameters.

                                        - Review the following command spec and identify the required arguments from the input schema.
                                        - Omit any arguments that are not required or do not apply to your use case.
                                        - Wrap all command arguments into the root "parameters" argument.
                                        - If required data is missing infer the data from your context or prompt the user as needed.
                                        - Run the tool again with the "command" and root "parameters" object.

                                        Command Spec:
                                        {childToolSpecJson}
                                        """
                            }
                        ]
                    };

                    foreach (ContentBlock contentBlock in callToolResult.Content)
                    {
                        finalResult.Content.Add(contentBlock);
                    }

                    return finalResult;
                }
            }

            return callToolResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception thrown while calling tool: {Tool}, command: {Command}", tool, command);
            return new CallToolResult
            {
                Content =
                [
                    new TextContentBlock {
                        Text = $"""
                            There was an error finding or calling tool and command.
                            Failed to call tool: {tool}, command: {command}
                            Error: {ex.Message}

                            Run again with the "learn=true" to get a list of available commands and their parameters.
                            """
                    }
                ]
            };
        }
    }

    private async Task<List<Tool>> GetChildToolListAsync(RequestContext<CallToolRequestParams> request, string tool, CancellationToken cancellationToken = default)
    {
        if (this._cachedToolLists.TryGetValue(tool, out var cachedList))
        {
            return cachedList;
        }

        if (string.IsNullOrWhiteSpace(request.Params?.Name))
        {
            throw new ArgumentNullException(nameof(request.Params.Name), "Tool name cannot be null or empty.");
        }

        McpClientOptions clientOptions = CreateClientOptions(request.Server);

        IMcpServerProvider mcpServerProvider = await this.FindServerProviderAsync(tool, cancellationToken);

        McpClient client = await mcpServerProvider.GetOrCreateClientAsync(tool, clientOptions, cancellationToken);

        if (client is null)
        {
            return [];
        }

        IList<McpClientTool> listTools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        if (listTools is null)
        {
            _logger.LogWarning("No tools found for tool: {Tool}", tool);
            return [];
        }

        List<Tool> tools = [.. listTools.Select(t => t.ProtocolTool)];

        this._cachedToolLists[tool] = tools;

        return tools;
    }

    private async Task<string> GetChildToolJsonAsync(RequestContext<CallToolRequestParams> request, string toolName, string commandName, CancellationToken cancellationToken = default)
    {
        List<Tool> tools = await GetChildToolListAsync(request, toolName, cancellationToken);

        return JsonSerializer.Serialize(tools);
    }
}