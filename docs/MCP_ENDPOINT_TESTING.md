# 测试 /mcp 端点指南

## 问题诊断

之前 `/mcp` 返回 404 的原因是：**`McpServerOptions` 在 `StdioToHttpProxyService` 初始化之前就被注册，导致调用 `CreateAggregatedServerOptions()` 时抛出异常。**

## 解决方案

修改启动流程，先构建临时 `ServiceProvider` 初始化 `StdioToHttpProxyService`，再注册 `McpServerOptions` 和 `AddMcpServer()`。

```csharp
// 1. 注册 StdioToHttpProxyService
builder.Services.AddStdioToHttpMcpServer();

// 2. 构建临时 ServiceProvider 并初始化
using var tempServiceProvider = builder.Services.BuildServiceProvider();
var proxyService = tempServiceProvider.GetRequiredService<StdioToHttpProxyService>();
await proxyService.InitializeAsync();

// 3. 创建并注册 McpServerOptions（现在可以安全调用）
McpServerOptions mcpServerOptions = proxyService.CreateAggregatedServerOptions();
builder.Services.AddSingleton(mcpServerOptions);

// 4. 注册 MCP Server
builder.Services.AddMcpServer().WithHttpTransport(...);
```

---

## 测试步骤

### 1. 启动服务器

```bash
cd src/McpProxy.StdioToSse.WebApi
dotnet run
```

预期输出：
```
info: Program[0]
      MCP Proxy HTTP/SSE Server started with 1 backend server(s)
info: Program[0]
      MCP endpoint: /mcp
info: Program[0]
      Health check: /health
info: Program[0]
      Server status: /api/servers
```

### 2. 测试健康检查（验证服务器正常运行）

```bash
curl http://localhost:5000/health
```

预期响应：
```
Healthy
```

### 3. 测试服务器状态端点

```bash
curl http://localhost:5000/api/servers
```

预期响应：
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
  "count": 1,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 4. 测试 MCP 端点（SSE 协议）

#### 方法 1：使用 curl（GET 请求）

```bash
curl -N -H "Accept: text/event-stream" http://localhost:5000/mcp
```

#### 方法 2：使用 MCP Inspector

```bash
npx @modelcontextprotocol/inspector http://localhost:5000/mcp
```

#### 方法 3：使用 HTTP POST 测试（JSON-RPC）

```bash
# 初始化连接
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }'

# 列出工具
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

预期响应（列出工具）：
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "echo",
        "description": "Echo back the input",
        "inputSchema": {
          "type": "object",
          "properties": {
            "message": {
              "type": "string"
            }
          }
        }
      }
    ]
  }
}
```

---

## 常见问题排查

### 问题 1：`/mcp` 仍然返回 404

**检查点：**
1. 确认服务器启动日志中有 `MCP endpoint: /mcp`
2. 检查端口是否正确（默认 5000）
3. 尝试访问其他端点（如 `/health` 或 `/api/servers`）确认服务器运行正常

**解决方案：**
- 清理并重新编译：`dotnet clean && dotnet build`
- 检查是否有端口冲突：`netstat -ano | findstr :5000`

### 问题 2：后端服务器连接失败

**日志示例：**
```
error: StdioToHttpProxyService[0]
      Failed to connect to server 'everything': Command not found: npx
```

**解决方案：**
- 确保已安装 Node.js 和 npx：`node --version && npx --version`
- 检查 `appsettings.json` 中的 `Command` 和 `Arguments` 配置
- 手动测试后端服务器：`npx -y @modelcontextprotocol/server-everything`

### 问题 3：MCP 协议握手失败

**解决方案：**
- 确保使用正确的协议版本：`2024-11-05`
- 检查 `initialize` 请求格式是否正确
- 查看服务器日志了解详细错误信息

---

## 验证清单

- [ ] 服务器成功启动并显示正确的端点信息
- [ ] `/health` 返回 `Healthy`
- [ ] `/api/servers` 返回后端服务器状态
- [ ] `/mcp` 端点可访问（不返回 404）
- [ ] MCP Inspector 可以成功连接
- [ ] 可以列出后端服务器的工具
- [ ] 可以成功调用工具

---

## 下一步

一旦确认 `/mcp` 端点正常工作，你可以：

1. **集成到 Claude Desktop**
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

2. **部署到生产环境**
   - 配置 HTTPS
   - 添加身份验证
   - 使用反向代理（Nginx/Caddy）

3. **监控和日志**
   - 启用结构化日志
   - 集成 Application Insights 或 Prometheus

---

## 参考资源

- [MCP 协议规范](https://spec.modelcontextprotocol.io/)
- [MCP Inspector 文档](https://github.com/modelcontextprotocol/inspector)
- [ASP.NET Core 端点路由](https://learn.microsoft.com/aspnet/core/fundamentals/routing)
