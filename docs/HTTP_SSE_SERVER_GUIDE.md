# MCP Proxy HTTP/SSE Server 使用指南

## 概述

`McpProxy.StdioToSse.WebApi` 现在作为 **HTTP/SSE MCP Server** 运行，将后端的 Stdio MCP 服务器聚合后通过 HTTP/SSE 协议暴露，支持 MCP Inspector、Claude Desktop 等客户端直接访问。

---

## 架构说明

```
MCP Client (Inspector/Claude)
    ↓ HTTP/SSE
McpProxy.StdioToSse.WebApi (:5000/mcp)
    ↓ 聚合转发
StdioToHttpProxyService
    ↓ Stdio
Backend MCP Servers (everything, filesystem, etc.)
```

---

## 快速开始

### 1. 配置后端 MCP 服务器

编辑 `appsettings.json`：

```json
{
  "McpServers": [
    {
      "Name": "everything",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-everything"],
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": false,
  "AllowServerFilter": true
}
```

### 2. 启动服务器

```bash
cd src/McpProxy.StdioToSse.WebApi
dotnet run
```

服务器将在 `http://localhost:5000` 启动。

---

## 使用 MCP Inspector 测试

### 方法 1：直接连接 HTTP/SSE 端点

```bash
npx @modelcontextprotocol/inspector http://localhost:5000/mcp
```

### 方法 2：使用配置文件

创建 `mcp-inspector.config.json`：

```json
{
  "mcpServers": {
    "mcp-proxy": {
      "url": "http://localhost:5000/mcp",
      "transport": "sse"
    }
  }
}
```

然后运行：

```bash
npx @modelcontextprotocol/inspector --config mcp-inspector.config.json
```

---

## 在 Claude Desktop 中使用

编辑 Claude Desktop 配置文件（`~/Library/Application Support/Claude/claude_desktop_config.json` macOS，或 `%APPDATA%\Claude\claude_desktop_config.json` Windows）：

```json
{
  "mcpServers": {
    "mcp-proxy": {
      "url": "http://localhost:5000/mcp",
      "transport": "sse"
    }
  }
}
```

重启 Claude Desktop 即可使用聚合后的所有工具。

---

## 可用端点

| 端点 | 说明 | 协议 |
|------|------|------|
| `/mcp` | MCP 协议端点 | HTTP/SSE |
| `/health` | 健康检查 | HTTP |
| `/servers` | 后端服务器状态 | HTTP |

---

## 查询后端服务器状态

```bash
curl http://localhost:5000/servers
```

返回示例：

```json
{
  "servers": [
    {
      "name": "everything",
      "connected": true,
      "serverInfo": "everything-server",
      "version": "1.0.0",
      "capabilities": {
        "tools": true,
        "prompts": true,
        "resources": true
      }
    }
  ],
  "count": 1
}
```

---

## 测试工具调用

使用 MCP Inspector 或直接 HTTP 请求测试：

```bash
# 列出工具
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'

# 调用工具
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"echo","arguments":{"message":"Hello"}}}'
```

---

## 配置选项

### UseNamespacePrefix

- `true`（默认）：多服务器时自动为工具名添加前缀（如 `everything:echo`）
- `false`：不添加前缀（仅单服务器或手动管理名称冲突时使用）

### AllowServerFilter

- `true`（默认）：允许客户端通过元数据指定查询特定服务器
- `false`：始终返回所有服务器的聚合结果

---

## 故障排除

### 1. 后端服务器连接失败

检查日志：

```
Failed to connect to server 'everything': Command not found: npx
```

**解决方案**：确保已安装 Node.js 和 npx，或检查 `Command` 和 `Arguments` 配置。

### 2. MCP Inspector 无法连接

确保：
- 服务器已启动并监听 `http://localhost:5000`
- 防火墙未阻止端口 5000
- 使用正确的 URL：`http://localhost:5000/mcp`

### 3. 工具列表为空

检查：
- 后端 MCP 服务器是否成功连接（访问 `/servers` 端点）
- 后端服务器是否支持 `tools/list` 方法

---

## 生产部署建议

1. **使用环境变量覆盖配置**：
   ```bash
   export MCPPROXY_UseNamespacePrefix=true
   export MCPPROXY_McpServers__0__Name=production-server
   ```

2. **配置 HTTPS**：
   ```csharp
   builder.WebHost.UseUrls("https://localhost:5001");
   ```

3. **添加身份验证**：
   ```csharp
   builder.Services.AddAuthentication()
       .AddJwtBearer(...);
   ```

4. **使用反向代理**（Nginx/Caddy）：
   ```nginx
   location /mcp {
       proxy_pass http://localhost:5000/mcp;
       proxy_http_version 1.1;
       proxy_set_header Upgrade $http_upgrade;
       proxy_set_header Connection "upgrade";
   }
   ```

---

## 示例：聚合多个 MCP 服务器

```json
{
  "McpServers": [
    {
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/path/to/dir"],
      "Enabled": true
    },
    {
      "Name": "github",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-github"],
      "Environment": {
        "GITHUB_TOKEN": "your-token"
      },
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": true
}
```

所有工具将自动聚合，前缀区分来源：
- `filesystem:read_file`
- `github:create_issue`

---

## 进一步阅读

- [MCP 协议规范](https://spec.modelcontextprotocol.io/)
- [MCP Inspector 文档](https://github.com/modelcontextprotocol/inspector)
- [ASP.NET Core 部署指南](https://learn.microsoft.com/aspnet/core/host-and-deploy/)

---

**维护者**: MCP Proxy Team  
**最后更新**: 2024
