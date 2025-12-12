# McpProxy.StdioToSse.WebApi

将本地 Stdio MCP 服务器转换为 HTTP/SSE API 的 Web 服务。

## 🚀 快速开始

### 运行服务

```bash
cd src/McpProxy.StdioToSse.WebApi
dotnet run
```

服务将在 `http://localhost:3000` 启动，Swagger UI 可在根路径访问。

### 配置示例

#### 单服务器模式

```json
{
  "HttpServer": {
    "Host": "localhost",
    "Port": 3000,
    "Stateless": false,
    "AllowedOrigins": ["*"]
  },
  "McpServers": [
    {
      "Name": "everything",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-everything"],
      "Environment": {
        "NODE_ENV": "development"
      },
      "WorkingDirectory": null,
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": false,
  "AllowServerFilter": true,
  "AutoReconnect": true,
  "HealthCheckInterval": 30
}
```

#### 多服务器模式

```json
{
  "HttpServer": {
    "Host": "0.0.0.0",
    "Port": 8080,
    "Stateless": false,
    "AllowedOrigins": ["*"]
  },
  "McpServers": [
    {
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "C:\\Users"],
      "Environment": {
        "NODE_ENV": "production"
      },
      "WorkingDirectory": null,
      "Enabled": true
    },
    {
      "Name": "github",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-github"],
      "Environment": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "ghp_xxxxx"
      },
      "WorkingDirectory": null,
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": true,
  "AllowServerFilter": true,
  "AutoReconnect": true,
  "HealthCheckInterval": 30
}
```

## 📚 API 文档

### Tools

- `POST /api/mcp/tools/list` - 列出所有工具
  - Query: `server` (可选) - 过滤特定服务器的工具
- `POST /api/mcp/tools/call` - 调用工具
  - Body: `{ "Name": "tool-name", "Arguments": {...} }`

### Prompts

- `POST /api/mcp/prompts/list` - 列出所有提示
  - Query: `server` (可选) - 过滤特定服务器的提示
- `POST /api/mcp/prompts/get` - 获取提示
  - Body: `{ "Name": "prompt-name", "Arguments": {...} }`

### Resources

- `POST /api/mcp/resources/list` - 列出所有资源
  - Query: `server` (可选) - 过滤特定服务器的资源
- `POST /api/mcp/resources/read` - 读取资源
  - Body: `{ "Uri": "resource-uri" }`

### Management

- `GET /api/servers` - 获取服务器状态
- `GET /api/capabilities` - 获取聚合能力
- `GET /health` - 健康检查

## 🔧 配置选项

### HttpServer

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Host` | string | localhost | 绑定地址 |
| `Port` | int | 3000 | 监听端口 |
| `Stateless` | bool | false | 无状态模式 |
| `AllowedOrigins` | string[] | - | CORS 允许的源 |

### McpServers

| 选项 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `Name` | string | 是 | 服务器名称 |
| `Command` | string | 是 | 启动命令 |
| `Arguments` | string[] | 否 | 命令参数 |
| `Environment` | object | 否 | 环境变量 |
| `WorkingDirectory` | string | 否 | 工作目录 |
| `Enabled` | bool | 否 | 是否启用（默认 true） |

### 全局选项

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `UseNamespacePrefix` | bool | true | 多服务器时使用命名空间前缀 |
| `AllowServerFilter` | bool | true | 允许按服务器过滤 |
| `AutoReconnect` | bool | true | 自动重连 |
| `HealthCheckInterval` | int | 30 | 健康检查间隔（秒） |

## 🌐 多服务器访问

当配置多个服务器并启用 `UseNamespacePrefix` 时：

### 工具调用
```json
{
  "Name": "filesystem:read_file",
  "Arguments": { "path": "/example.txt" }
}
```

### 资源读取
```json
{
  "Uri": "github:repo://owner/repo/README.md"
}
```

### 服务器过滤
```http
POST /api/mcp/tools/list?server=filesystem
```

## 🐳 Docker 部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
EXPOSE 3000
ENTRYPOINT ["dotnet", "McpProxy.StdioToSse.WebApi.dll"]
```

```bash
docker run -p 3000:3000 \
  -e HttpServer__Port=3000 \
  -e McpServers__0__Command=npx \
  -e McpServers__0__Arguments__0=-y \
  -e McpServers__0__Arguments__1=@modelcontextprotocol/server-everything \
  mcp-proxy-webapi
```

## 🔗 相关文档

- **MCP 官方文档**: https://modelcontextprotocol.io/
- **Swagger UI**: http://localhost:3000 (运行时访问)
- **健康检查**: http://localhost:3000/health

## 📝 版本历史

- **v1.0.0** (2025-12-09)
  - ✅ 初始发布
  - ✅ 支持多服务器聚合
  - ✅ Swagger UI 自动文档
  - ✅ 健康检查和状态监控
  - ✅ 统一配置格式（与 CLI 项目一致）
