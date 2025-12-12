# 配置文件说明

## 📁 目录结构

```
McpProxy.Cli/
├── appsettings.json          # 默认配置文件（仅日志配置）
└── config-examples/          # 配置示例目录
    ├── basic.example.json            # 基础配置示例
    ├── sse-to-stdio.example.json     # SSE→Stdio 模式示例
    ├── stdio-to-http.example.json    # Stdio→HTTP 模式示例
    ├── multi-stdio.example.json      # 多服务器配置示例
    ├── oauth2.example.json           # OAuth2 认证示例
    └── test.example.json             # 测试配置示例
```

## 🎯 推荐使用方式

### 方式 1: 命令行参数（推荐）✅

直接使用命令行参数，无需配置文件：

```bash
# SSE→Stdio 模式
mcp-proxy sse-to-stdio https://api.example.com/sse --access-token "your-token"

# Stdio→HTTP 模式
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-everything --port 8080
```

### 方式 2: 配置文件

适用于复杂配置或生产环境：

```bash
# 复制示例配置
cp config-examples/sse-to-stdio.example.json my-config.json

# 编辑配置文件
# ...

# 使用配置文件运行
mcp-proxy config my-config.json
```

## 📖 配置示例说明

### 1. `basic.example.json` - 基础配置

最简单的 Stdio→HTTP 配置，适合快速开始。

**使用场景**: 本地开发、快速测试

```bash
mcp-proxy config config-examples/basic.example.json
```

### 2. `sse-to-stdio.example.json` - SSE 到 Stdio

连接远程 SSE 服务器并通过 Stdio 暴露。

**使用场景**: Claude Desktop 连接远程 MCP 服务

**配置项**:
- `Mode`: "SseToStdio"
- `SseClient.Url`: 远程 SSE 服务器地址
- `SseClient.AccessToken`: Bearer Token 认证
- `SseClient.VerifySsl`: SSL 证书验证

**命令行等效**:
```bash
mcp-proxy sse-to-stdio https://api.example.com/sse --access-token "token"
```

### 3. `stdio-to-http.example.json` - Stdio 到 HTTP

启动 HTTP 服务器，代理到本地 Stdio MCP 服务器。

**使用场景**: 将本地 MCP 服务器暴露为 HTTP API

**配置项**:
- `Mode`: "StdioToHttp"
- `McpServers[]`: 服务器列表
- `HttpServer.Port`: HTTP 端口
- `HttpServer.AllowedOrigins`: CORS 配置

**命令行等效**:
```bash
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-everything --port 8080
```

### 4. `multi-stdio.example.json` - 多服务器配置

同时运行多个 Stdio MCP 服务器，聚合暴露。

**使用场景**: 
- 管理多个 MCP 服务器
- 提供统一的 API 入口

**特性**:
- 支持命名服务器
- 自动命名空间前缀
- 独立的环境变量配置

**访问路径**:
- 默认: `http://localhost:8080/sse`
- 命名服务器: `http://localhost:8080/servers/{name}/sse`

### 5. `oauth2.example.json` - OAuth2 认证

使用 OAuth2 客户端凭据流连接远程服务。

**使用场景**: 企业环境、需要 OAuth2 认证的远程服务

**配置项**:
- `SseClient.OAuth2.ClientId`: 客户端 ID
- `SseClient.OAuth2.ClientSecret`: 客户端密钥
- `SseClient.OAuth2.TokenUrl`: 令牌端点
- `SseClient.OAuth2.Scope`: 授权范围

**命令行等效**:
```bash
mcp-proxy sse-to-stdio https://api.example.com/sse \
  --client-id "client-id" \
  --client-secret "secret" \
  --token-url "https://auth.example.com/token" \
  --scope "api.read"
```

### 6. `test.example.json` - 测试配置

用于自动化测试和 CI/CD 环境的配置。

**使用场景**: 单元测试、集成测试、CI/CD

## 🔧 使用配置文件的步骤

### 步骤 1: 选择合适的示例

根据您的使用场景选择对应的示例配置：

| 场景 | 示例文件 |
|------|----------|
| 连接远程 SSE 服务器 | `sse-to-stdio.example.json` |
| 启动本地 HTTP 服务 | `stdio-to-http.example.json` |
| 管理多个服务器 | `multi-stdio.example.json` |
| OAuth2 认证 | `oauth2.example.json` |

### 步骤 2: 复制并修改

```bash
# 复制示例配置
cp config-examples/sse-to-stdio.example.json production.json

# 编辑配置文件，填写实际的参数
notepad production.json  # Windows
nano production.json     # Linux/macOS
```

### 步骤 3: 运行

```bash
mcp-proxy config production.json
```

## 📝 配置文件结构

### SSE→Stdio 模式

```json
{
  "Mode": "SseToStdio",
  "SseClient": {
    "Url": "https://api.example.com/sse",
    "AccessToken": "your-token",
    "VerifySsl": true,
    "Headers": {
      "X-Custom-Header": "value"
    },
    "OAuth2": {
      "ClientId": "client-id",
      "ClientSecret": "client-secret",
      "TokenUrl": "https://auth.example.com/token",
      "Scope": "api.read"
    }
  }
}
```

### Stdio→HTTP 模式

```json
{
  "Mode": "StdioToHttp",
  "HttpServer": {
    "Host": "localhost",
    "Port": 8080,
    "Stateless": false,
    "AllowedOrigins": ["*"]
  },
  "McpServers": [
    {
      "Name": "server-name",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-everything"],
      "Environment": {
        "NODE_ENV": "production"
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

## 🔐 安全建议

### 1. 不要提交敏感信息到版本控制

```bash
# .gitignore
*.json
!appsettings.json
!config-examples/*.json
```

### 2. 使用环境变量

```bash
# 设置环境变量
export API_ACCESS_TOKEN="your-secret-token"

# 在配置文件中引用（需要代码支持）
{
  "SseClient": {
    "AccessToken": "${API_ACCESS_TOKEN}"
  }
}
```

### 3. 使用命令行参数

推荐使用命令行参数而非配置文件存储敏感信息：

```bash
mcp-proxy sse-to-stdio https://api.example.com/sse \
  --access-token "$API_ACCESS_TOKEN"
```

## 💡 最佳实践

### 开发环境
- ✅ 使用命令行参数快速测试
- ✅ 将配置文件加入 `.gitignore`

### 生产环境
- ✅ 使用配置文件管理复杂配置
- ✅ 通过环境变量注入敏感信息
- ✅ 使用配置管理工具（如 Kubernetes ConfigMap）

### 团队协作
- ✅ 提供示例配置文件（`.example.json`）
- ✅ 文档化配置项说明
- ✅ 敏感信息使用占位符

## ❓ 常见问题

### Q: 为什么默认的 `appsettings.json` 是空的？

A: 推荐使用命令行参数而非配置文件，这样更灵活且不会泄露敏感信息。配置文件适用于复杂场景。

### Q: 如何在多个环境使用不同配置？

A: 创建不同的配置文件：

```bash
mcp-proxy config config/development.json  # 开发环境
mcp-proxy config config/production.json   # 生产环境
```

### Q: 配置文件和命令行参数可以混用吗？

A: 目前不支持。选择一种方式：
- 简单场景 → 命令行参数
- 复杂场景 → 配置文件

## 🔗 相关文档

- **CLI 使用指南**: `../README.md`
- **优化总结**: `../../docs/MCPPROXY_CLI_OPTIMIZATION.md`
- **快速开始**: `../../docs/QUICK_START_CLI.md`

---

**更新时间**: 2025-12-09  
**版本**: v1.0.0
