# 📝 配置文件示例

本目录包含 MCP Proxy 各组件的配置示例。

## 📂 目录结构

```
config-examples/
├── StdioToSse.WebApi/        # Web API 配置示例
│   ├── servers.json          # 服务器配置示例
│   └── production.json       # 生产环境配置
│
└── SseToStdio.Host/          # Host 服务配置示例
    ├── oauth2.json           # OAuth2 认证
    └── no-ssl-verify.json    # 开发环境（禁用SSL验证）
```

## 🚀 如何使用

### 方法 1: 复制到项目目录

```bash
# 复制服务器配置
cp config-examples/StdioToSse.WebApi/servers.json \
   src/McpProxy.StdioToSse.WebApi/appsettings.json

# 复制 OAuth2 配置
cp config-examples/SseToStdio.Host/oauth2.json \
   src/McpProxy.SseToStdio.Host/appsettings.json
```

### 方法 2: 环境变量覆盖

```bash
# HTTP 服务器配置
export MCPPROXY_HttpServer__Port=8080
export MCPPROXY_HttpServer__Host=0.0.0.0

# SSE 客户端配置
export MCPPROXY_SseClient__Url=https://api.example.com/mcp/sse

# Stdio 服务器配置
export MCPPROXY_StdioServers__UseNamespacePrefix=true

# OAuth2 配置
export MCPPROXY_SseClient__OAuth2__ClientId=your-id
export MCPPROXY_SseClient__OAuth2__ClientSecret=your-secret

dotnet run
```

### 方法 3: 命令行参数

```bash
dotnet run --HttpServer:Port=3000 --SseClient:Url=https://api.example.com
```

## 📋 配置说明

### StdioToSse.WebApi

| 文件 | 用途 | 适用场景 |
|------|------|----------|
| `servers.json` | 服务器配置示例 | 开发/生产 |
| `production.json` | 生产环境优化 | 生产部署 |

**配置节说明：**
- `HttpServer` - HTTP 服务器配置（端口、CORS等）
- `StdioServers` - Stdio 服务器配置（服务器列表、命名空间等）

### SseToStdio.Host

| 文件 | 用途 | 适用场景 |
|------|------|----------|
| `oauth2.json` | OAuth2 客户端凭据认证 | 企业安全 |
| `no-ssl-verify.json` | 禁用SSL验证 | 本地开发 |

**配置节说明：**
- `SseClient` - SSE 客户端配置（URL、认证等）

## ⚠️ 安全提示

1. **敏感信息使用环境变量**
   - Token、密码等不要硬编码
   - 配置文件中使用 `${ENV_VAR}` 占位符

2. **生产环境检查清单**
   - ✅ `VerifySsl: true`
   - ✅ 限制 CORS 源（不使用 `*`）
   - ✅ 使用 HTTPS 端点
   - ✅ 使用 OAuth2 而非静态 Token

## 📖 更多文档

- [Web API 文档](../src/McpProxy.StdioToSse.WebApi/README.md)
- [Host 服务文档](../src/McpProxy.SseToStdio.Host/README.md)
