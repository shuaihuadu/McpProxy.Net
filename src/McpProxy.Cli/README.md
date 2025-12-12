# MCP Proxy CLI 使用指南

## 📋 概述

`mcp-proxy` 是一个用于 MCP (Model Context Protocol) 协议转换的命令行工具，支持在不同传输协议之间进行转换。

## 🚀 快速开始

### 安装

```bash
# 构建项目
cd src/McpProxy.Cli
dotnet build

# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### 基本命令

```bash
# 查看帮助
mcp-proxy --help

# 查看子命令帮助
mcp-proxy sse-to-stdio --help
mcp-proxy stdio-to-sse --help
```

## 📚 使用模式

### 模式 1: SSE → Stdio

将远程 SSE MCP 服务器转换为本地 Stdio 接口，适用于 Claude Desktop 等客户端。

#### 基本用法

```bash
# 连接到远程 SSE 服务器
mcp-proxy sse-to-stdio https://api.example.com/sse
```

#### 使用 Bearer Token 认证

```bash
mcp-proxy sse-to-stdio https://api.example.com/sse \
  --access-token "your-token-here"
```

#### 使用 OAuth2 认证

```bash
mcp-proxy sse-to-stdio https://api.example.com/sse \
  --client-id "your-client-id" \
  --client-secret "your-client-secret" \
  --token-url "https://auth.example.com/oauth2/token" \
  --scope "api.read"
```

#### 添加自定义 HTTP 头

```bash
mcp-proxy sse-to-stdio https://api.example.com/sse \
  --header "X-API-Key=your-key" \
  --header "X-Custom-Header=value"
```

#### 禁用 SSL 验证（仅用于开发环境）

```bash
mcp-proxy sse-to-stdio https://localhost:8080/sse \
  --verify-ssl false
```

### 模式 2: Stdio → SSE

启动 HTTP/SSE 服务器，将请求代理到本地 Stdio MCP 服务器。

#### 基本用法

```bash
# 启动 SSE 服务器，代理到 MCP 服务器
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-everything
```

#### 指定端口和主机

```bash
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-fetch \
  --port 8080 \
  --host 0.0.0.0
```

#### 启用 CORS

```bash
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-fetch \
  --port 8080 \
  --allow-origin "*"
```

#### 设置环境变量

```bash
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-github \
  --env "GITHUB_PERSONAL_ACCESS_TOKEN=ghp_xxxxx" \
  --env "NODE_ENV=production"
```

#### 设置工作目录

```bash
mcp-proxy stdio-to-sse python server.py \
  --cwd "/path/to/project"
```

#### 启用无状态模式

```bash
mcp-proxy stdio-to-sse uvx mcp-server-fetch \
  --stateless
```

### 模式 3: 使用配置文件

使用 JSON 配置文件运行，适合复杂场景。

```bash
mcp-proxy config appsettings.json
```

#### 配置文件示例 (SSE→Stdio)

```json
{
  "Mode": "SseToStdio",
  "SseClient": {
    "Url": "https://api.example.com/sse",
    "AccessToken": "your-token",
    "VerifySsl": true,
    "Headers": {
      "X-API-Key": "your-key"
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

#### 配置文件示例 (Stdio→SSE，单服务器)

```json
{
  "Mode": "StdioToHttp",
  "HttpServer": {
    "Host": "localhost",
    "Port": 3000,
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
    }
  ],
  "UseNamespacePrefix": false,
  "AllowServerFilter": true,
  "AutoReconnect": true,
  "HealthCheckInterval": 30
}
```

#### 配置文件示例 (Stdio→SSE，多服务器)

```json
{
  "Mode": "StdioToHttp",
  "HttpServer": {
    "Host": "0.0.0.0",
    "Port": 8080,
    "AllowedOrigins": ["*"]
  },
  "McpServers": [
    {
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/path"]
    },
    {
      "Name": "github",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-github"],
      "Environment": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "ghp_xxxxx"
      }
    },
    {
      "Name": "fetch",
      "Command": "uvx",
      "Arguments": ["mcp-server-fetch"]
    }
  ],
  "UseNamespacePrefix": true,
  "AllowServerFilter": true
}
```

## 🛠️ 命令行选项

### 全局选项

| 选项 | 简写 | 默认值 | 说明 |
|------|------|--------|------|
| `--log-level` | `-l` | Information | 日志级别 (Trace, Debug, Information, Warning, Error, Critical) |
| `--debug` | `-d` | false | 启用调试模式 |
| `--help` | `-h` | - | 显示帮助信息 |
| `--version` | - | - | 显示版本信息 |

### SSE→Stdio 模式选项

| 选项 | 说明 |
|------|------|
| `url` (必需) | 远程 SSE 服务器 URL |
| `--access-token` | Bearer Token 认证令牌 |
| `--header` / `-H` | 自定义 HTTP 头 (可多次使用) |
| `--verify-ssl` | 是否验证 SSL 证书 (默认 true) |
| `--client-id` | OAuth2 客户端 ID |
| `--client-secret` | OAuth2 客户端密钥 |
| `--token-url` | OAuth2 令牌端点 URL |
| `--scope` | OAuth2 作用域 |

### Stdio→SSE 模式选项

| 选项 | 默认值 | 说明 |
|------|--------|------|
| `command` (必需) | - | MCP 服务器命令和参数 |
| `--port` | 3000 | HTTP 服务器端口 |
| `--host` | localhost | HTTP 服务器主机 |
| `--stateless` | false | 启用无状态模式 |
| `--allow-origin` | - | 允许的 CORS 源 (可多次使用) |
| `--env` / `-e` | - | 环境变量 (可多次使用) |
| `--cwd` | - | 工作目录 |
| `--use-namespace-prefix` | true | 多服务器模式下使用命名空间前缀 |

## 📖 使用场景

### 场景 1: Claude Desktop 连接远程 SSE 服务器

```json
// Claude Desktop 配置文件
{
  "mcpServers": {
    "remote-server": {
      "command": "mcp-proxy",
      "args": [
        "sse-to-stdio",
        "https://api.example.com/sse",
        "--access-token",
        "${API_ACCESS_TOKEN}"
      ],
      "env": {
        "API_ACCESS_TOKEN": "your-token-here"
      }
    }
  }
}
```

### 场景 2: 将本地工具暴露为远程服务

```bash
# 启动服务器
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-filesystem C:\\Users \\
  --port 8080 \\
  --host 0.0.0.0 \\
  --allow-origin "*"

# 其他客户端可以通过 http://your-server:8080/sse 访问
```

### 场景 3: 开发环境测试

```bash
# 启用调试日志
mcp-proxy stdio-to-sse npx -y @modelcontextprotocol/server-everything \\
  --log-level Debug \\
  --debug
```

### 场景 4: 企业环境（OAuth2 认证）

```bash
mcp-proxy sse-to-stdio https://corporate-mcp.company.com/sse \\
  --client-id "app-client-id" \\
  --client-secret "app-client-secret" \\
  --token-url "https://auth.company.com/oauth2/token" \\
  --scope "mcp.read mcp.write"
```

## 🐛 故障排除

### 问题 1: "找不到命令"

**解决方案**: 确保 mcp-proxy 在 PATH 中

```bash
# Windows
where mcp-proxy

# Linux/macOS
which mcp-proxy

# 或使用完整路径
C:\\path\\to\\mcp-proxy.exe sse-to-stdio ...
```

### 问题 2: "SSL certificate verify failed"

**解决方案**: 对于自签名证书，禁用 SSL 验证

```bash
mcp-proxy sse-to-stdio https://localhost:8080/sse --verify-ssl false
```

### 问题 3: "Connection refused"

**检查项**:
1. 确认 URL 正确
2. 确认服务器正在运行
3. 检查防火墙设置
4. 验证认证信息

### 问题 4: "服务器无法启动"

**解决方案**: 检查端口是否被占用

```bash
# Windows
netstat -ano | findstr :3000

# Linux/macOS
lsof -i :3000
```

## 💡 最佳实践

### 1. 使用环境变量存储敏感信息

```bash
# 设置环境变量
export API_ACCESS_TOKEN="your-secret-token"

# 使用环境变量
mcp-proxy sse-to-stdio https://api.example.com/sse \\
  --access-token "$API_ACCESS_TOKEN"
```

### 2. 使用配置文件管理复杂配置

对于生产环境，推荐使用配置文件而非命令行参数：

```bash
mcp-proxy config production.json
```

### 3. 日志级别建议

- **开发**: `--log-level Debug`
- **测试**: `--log-level Information`
- **生产**: `--log-level Warning`

### 4. CORS 配置建议

- **开发**: `--allow-origin "*"`
- **生产**: 指定具体域名

```bash
mcp-proxy stdio-to-sse ... \\
  --allow-origin "https://app.example.com" \\
  --allow-origin "https://admin.example.com"
```

## 🔗 相关资源

- **MCP 官方文档**: https://modelcontextprotocol.io/
- **MCP Inspector**: https://modelcontextprotocol.io/docs/tools/inspector
- **项目 GitHub**: https://github.com/yourname/mcp_proxy

## 📝 版本历史

- **v1.0.0** (2025-12-09)
  - ✅ 初始发布
  - ✅ 支持 SSE→Stdio 和 Stdio→SSE 模式
  - ✅ 命令行参数解析
  - ✅ OAuth2 认证支持
  - ✅ 配置文件支持
