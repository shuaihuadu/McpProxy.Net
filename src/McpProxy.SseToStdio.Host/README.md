# McpProxy.SseToStdio.Host

将远程 SSE MCP 服务器转换为本地 Stdio 接口的后台服务。

## 🚀 快速开始

### 运行服务

```bash
cd McpProxy.SseToStdio.Host
dotnet run
```

服务将连接到远程 SSE 服务器并通过 stdin/stdout 暴露 MCP 接口。

### 配置示例

#### Bearer Token 认证

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "Headers": {
      "Authorization": "Bearer YOUR_TOKEN"
    },
    "VerifySsl": true
  }
}
```

#### OAuth2 客户端凭据流

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "OAuth2": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "TokenUrl": "https://auth.example.com/oauth2/token",
      "Scope": "api.read"
    }
  }
}
```

#### 使用环境变量

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "AccessToken": null
  }
}
```

然后设置环境变量：
```bash
export API_ACCESS_TOKEN="your-token-here"
dotnet run
```

## 🔧 作为系统服务运行

### Windows Service

```bash
# 发布
dotnet publish -c Release -r win-x64 --self-contained

# 安装服务
sc create McpProxySseToStdio binPath="C:\path\to\McpProxy.SseToStdio.Host.exe"

# 启动服务
sc start McpProxySseToStdio
```

### Linux systemd

创建服务文件 `/etc/systemd/system/mcp-proxy-ssetostdio.service`：

```ini
[Unit]
Description=MCP Proxy SSE to Stdio Service
After=network.target

[Service]
Type=notify
ExecStart=/opt/mcp-proxy/McpProxy.SseToStdio.Host
WorkingDirectory=/opt/mcp-proxy
Restart=always
RestartSec=10
User=mcp-proxy
Environment=MCPPROXY_SseClient__AccessToken=your-token

[Install]
WantedBy=multi-user.target
```

启动服务：

```bash
sudo systemctl daemon-reload
sudo systemctl enable mcp-proxy-ssetostdio
sudo systemctl start mcp-proxy-ssetostdio
sudo systemctl status mcp-proxy-ssetostdio
```

## 📝 配置选项

### SseClient

| 选项 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `Url` | string | ✅ | SSE 服务器 URL |
| `Headers` | object | ❌ | 自定义 HTTP 头 |
| `VerifySsl` | bool | ❌ | 验证 SSL 证书（默认 true） |
| `AccessToken` | string | ❌ | Bearer Token |
| `OAuth2` | object | ❌ | OAuth2 配置 |

### OAuth2 配置

| 选项 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `ClientId` | string | ✅ | OAuth2 客户端 ID |
| `ClientSecret` | string | ✅ | OAuth2 客户端密钥 |
| `TokenUrl` | string | ✅ | Token 端点 URL |
| `Scope` | string | ❌ | 请求的作用域 |

## 🐳 Docker 部署

```bash
# 构建镜像
docker build -t mcp-proxy-host -f Dockerfile.Host .

# 运行容器
docker run -i \
  -e MCPPROXY_SseClient__Url=https://api.example.com/mcp/sse \
  -e MCPPROXY_SseClient__AccessToken=your-token \
  mcp-proxy-host
```

## 🔍 日志配置

### 详细日志

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "McpProxy": "Trace"
    }
  }
}
```

### 查看日志（systemd）

```bash
sudo journalctl -u mcp-proxy-ssetostdio -f
```

## 🛡️ 安全建议

1. **使用 OAuth2** 而不是长期 Token
2. **启用 SSL 验证** (`VerifySsl: true`)
3. **使用环境变量** 存储敏感信息
4. **限制服务权限** (非 root 用户运行)
5. **定期轮换凭据**

## 🔄 自动重连

服务会自动处理连接断开并重试：

- 网络错误时自动重连
- Token 过期时自动刷新（OAuth2）
- 优雅关闭支持

## 📊 监控

### 健康检查

服务状态通过退出码反映：
- `0`: 正常停止
- `1`: 异常终止

### 指标（未来支持）

计划添加：
- Prometheus metrics 端点
- 连接状态指标
- 请求统计

## 🐛 故障排除

### 连接失败

```bash
# 检查网络连接
curl -I https://api.example.com/mcp/sse

# 验证 Token
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://api.example.com/mcp/sse
```

### OAuth2 认证失败

```bash
# 测试 Token 端点
curl -X POST https://auth.example.com/oauth2/token \
  -d "grant_type=client_credentials" \
  -d "client_id=your-id" \
  -d "client_secret=your-secret"
```

### 查看详细错误

```bash
# 启用 Trace 日志
export MCPPROXY_Logging__LogLevel__McpProxy=Trace
dotnet run
```
