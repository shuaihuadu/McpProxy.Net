using McpProxy.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpProxy.Core
{
    /// <summary>
    /// 从 JSON 文件载入命名的 stdio 服务器配置（等同于 Python 的 config_loader）
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 从指定路径载入 named server 配置，并把 baseEnv 继承合并到每个条目中
        /// </summary>
        /// <param name="configFilePath">JSON 配置文件路径</param>
        /// <param name="baseEnv">基础环境变量（会被复制并与每个 server 的 env 合并）</param>
        /// <param name="logger">可选的 ILogger，用于记录信息与错误</param>
        /// <returns>命名到 StdioServerParameters 的字典</returns>
        /// <exception cref="FileNotFoundException">文件未找到时抛出</exception>
        /// <exception cref="JsonException">JSON 解析错误时抛出</exception>
        /// <exception cref="ArgumentException">格式不合法时抛出</exception>
        public static async Task<IDictionary<string, StdioServerParameter>> LoadNamedServerConfigsFromFileAsync(
            string configFilePath,
            IDictionary<string, string>? baseEnv,
            ILogger? logger = null)
        {
            if (configFilePath is null)
            {
                throw new ArgumentNullException(nameof(configFilePath));
            }

            IDictionary<string, StdioServerParameter> namedStdioParams = new Dictionary<string, StdioServerParameter>(StringComparer.OrdinalIgnoreCase);

            logger?.LogInformation("Loading named server configurations from: {Path}", configFilePath);

            string jsonText;
            try
            {
                //using FileStream fileStream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                //using StreamReader reader = new StreamReader(fileStream);

                //jsonText = await reader.ReadToEndAsync().ConfigureAwait(false);

                jsonText = await File.ReadAllTextAsync(configFilePath);
            }
            catch (FileNotFoundException)
            {
                logger?.LogError("Configuration file not found: {Path}", configFilePath);
                throw;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error opening or reading configuration file {Path}, error: {Message}", configFilePath, ex.Message);
                throw new ArgumentException($"Could not read configuration file: {ex.Message}", ex);
            }

            JsonDocument root;
            try
            {
                root = JsonDocument.Parse(jsonText);
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Error decoding JSON from configuration file: {Path}", configFilePath);
                throw;
            }

            using (root)
            {
                if (root.RootElement.ValueKind != JsonValueKind.Object ||
                    !root.RootElement.TryGetProperty("mcpServers", out JsonElement serversElement))
                {
                    string message = $"Invalid config file format in {configFilePath}. Missing 'mcpServers' key.";
                    logger?.LogError(message);
                    throw new ArgumentException(message);
                }

                if (serversElement.ValueKind != JsonValueKind.Object)
                {
                    string message = $"Invalid 'mcpServers' section in {configFilePath}, expected object.";
                    logger?.LogError(message);
                    throw new ArgumentException(message);
                }

                foreach (JsonProperty serverProp in serversElement.EnumerateObject())
                {
                    string name = serverProp.Name;
                    JsonElement serverConfig = serverProp.Value;

                    if (serverConfig.ValueKind != JsonValueKind.Object)
                    {
                        logger?.LogWarning("Skipping invalid server config for '{Name}' in {Path}. Entry is not an object.", name, configFilePath);
                        continue;
                    }

                    // enabled (default true)
                    if (serverConfig.TryGetProperty("enabled", out JsonElement enabledElement) &&
                        enabledElement.ValueKind == JsonValueKind.False)
                    {
                        logger?.LogInformation("Named server '{Name}' from config is not enabled. Skipping.", name);
                        continue;
                    }

                    // command (required)
                    string? command = null;
                    if (serverConfig.TryGetProperty("command", out JsonElement commandElement) &&
                        commandElement.ValueKind == JsonValueKind.String)
                    {
                        command = commandElement.GetString();
                    }

                    if (string.IsNullOrWhiteSpace(command))
                    {
                        logger?.LogWarning("Named server '{Name}' from config is missing 'command'. Skipping.", name);
                        continue;
                    }

                    // args (optional list)
                    List<string> commandArgs = new List<string>();
                    if (serverConfig.TryGetProperty("args", out JsonElement argsElement))
                    {
                        if (argsElement.ValueKind != JsonValueKind.Array)
                        {
                            logger?.LogWarning("Named server '{Name}' from config has invalid 'args' (must be an array). Skipping.", name);
                            continue;
                        }

                        foreach (JsonElement arg in argsElement.EnumerateArray())
                        {
                            if (arg.ValueKind == JsonValueKind.String)
                            {
                                commandArgs.Add(arg.GetString() ?? string.Empty);
                            }
                            else
                            {
                                // 非字符串的 args 被转为其 JSON 文本表示以保证健壮性
                                commandArgs.Add(arg.ToString());
                            }
                        }
                    }

                    // env (optional object)
                    Dictionary<string, string> env = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (baseEnv != null)
                    {
                        foreach (KeyValuePair<string, string> kv in baseEnv)
                        {
                            env[kv.Key] = kv.Value;
                        }
                    }

                    if (serverConfig.TryGetProperty("env", out JsonElement envElement))
                    {
                        if (envElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (JsonProperty envProp in envElement.EnumerateObject())
                            {
                                if (envProp.Value.ValueKind == JsonValueKind.String)
                                {
                                    env[envProp.Name] = envProp.Value.GetString() ?? string.Empty;
                                }
                                else
                                {
                                    // 将非字符串值序列化为文本表示
                                    env[envProp.Name] = envProp.Value.ToString() ?? string.Empty;
                                }
                            }
                        }
                        else
                        {
                            logger?.LogWarning("Named server '{Name}' from config has invalid 'env' (must be an object). Ignoring 'env'.", name);
                        }
                    }

                    string? cwd = null;
                    StdioServerParameter parameters = new StdioServerParameter(command: command, args: commandArgs.AsReadOnly(), env: env, cwd: cwd);

                    namedStdioParams[name] = parameters;

                    logger?.LogInformation("Configured named server '{Name}' from config: {Command} {Args}",
                        name,
                        command,
                        string.Join(" ", commandArgs));
                }
            }

            return namedStdioParams;
        }
    }
}
