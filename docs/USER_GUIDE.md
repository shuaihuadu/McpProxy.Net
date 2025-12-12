# MCP Proxy 用户指南

> **面向对象**: 最终用户、运维人员  
> **文档版本**: v2.0  
> **最后更新**: 2025-12-09

---

## 📋 目录

- [1. 快速开始](#1-快速开始)
- [2. 配置说明](#2-配置说明)
- [3. 使用方式](#3-使用方式)
- [4. API 参考](#4-api-参考)
- [5. 部署指南](#5-部署指南)
- [6. 故障排除](#6-故障排除)
- [7. 最佳实践](#7-最佳实践)

---

## 1. 快速开始

### 1.1 前置要求

| 要求 | 说明 |
|------|------|
| **.NET Runtime** | 10.0 或更高版本 |
| **操作系统** | Windows 10+、Linux、macOS |
| **内存** | 最小 512MB，推荐 1GB+ |
| **MCP 服务器** | 本地或远程 MCP 服务器 |

### 1.2 安装

#### 方式 1: 下载发布版

```bash
# 下载最新版本
wget https://github.com/your-org/mcp-proxy/releases/latest/download/mcp-proxy-linux-x64.zip

# 解压
unzip mcp-proxy-linux-x64.zip
cd mcp-proxy

# 运行
./McpProxy.StdioToSse.WebApi
```

#### 方式 2: 使用 Docker

```bash
# 拉取镜像
docker pull your-org/mcp-proxy:latest

# 运行容器
docker run -d \
  -p 3000:3000 \
  -v $(pwd)/appsettings.json:/app/appsettings.json \
  your-org/mcp-proxy:latest
```

#### 方式 3: 从源码构建

```bash
# 克隆仓库
git clone <repository-url>
cd mcp_proxy

# 构建
dotnet build -c Release

# 运行
cd src/McpProxy.StdioToSse.WebApi
dotnet run
```

### 1.3 验证安装

```bash
# 检查健康状态
curl http://localhost:3000/health

# 访问 Swagger UI
# 打开浏览器: http://localhost:3000
```

---

## 2. 配置说明

### 2.1 配置文件位置

| 应用 | 配置文件 |
|------|---------|
| **Web API** | `src/McpProxy.StdioToSse.WebApi/appsettings.json` |
| **Host Service** | `src/McpProxy.SseToStdio.Host/appsettings.json` |
| **CLI** | 命令行参数或 `appsettings.json` |

### 2.2 Web API 配置 (Stdio → HTTP/SSE)

#### 完整示例

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
      "Name": "filesystem",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/Users/username"],
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
        "GITHUB_TOKEN": "your-token-here"
      },
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": true,
  "AllowServerFilter": true,
  "AutoReconnect": true,
  "HealthCheckInterval": 30,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### 配置项说明

**HttpServer**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Host` | string | `"localhost"` | HTTP 服务器监听地址 |
| `Port` | int | `3000` | HTTP 服务器端口 (1-65535) |
| `Stateless` | bool | `false` | 是否启用无状态模式 |
| `AllowedOrigins` | string[] | `["*"]` | CORS 允许的源，`*` 表示所有 |

**McpServers[]**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Name` | string | 是 | 服务器唯一名称（字母开头，字母数字-_） |
| `Command` | string | 是 | 可执行命令（如 `npx`, `python`, `node`） |
| `Arguments` | string[] | 否 | 命令行参数列表 |
| `Environment` | object | 否 | 环境变量键值对 |
| `WorkingDirectory` | string | 否 | 工作目录，默认当前目录 |
| `Enabled` | bool | 否 | 是否启用，默认 `true` |

**全局配置**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `UseNamespacePrefix` | bool | `true` | 多服务器时是否添加前缀 |
| `AllowServerFilter` | bool | `true` | 是否允许按服务器过滤 |
| `AutoReconnect` | bool | `true` | 断开时是否自动重连 |
| `HealthCheckInterval` | int | `30` | 健康检查间隔（秒，5-600） |

#### 示例配置

**单个服务器**

```json
{
  "McpServers": [
    {
      "Name": "default",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-everything"]
    }
  ],
  "UseNamespacePrefix": false
}
```

**多个服务器（聚合）**

```json
{
  "McpServers": [
    {
      "Name": "fs",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/"]
    },
    {
      "Name": "db",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-sqlite", "data.db"]
    },
    {
      "Name": "web",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-brave-search"]
    }
  ],
  "UseNamespacePrefix": true
}
```

### 2.3 Host Service 配置 (SSE → Stdio)

#### 完整示例

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "AccessToken": "your-bearer-token",
    "Headers": {
      "X-Custom-Header": "value"
    },
    "VerifySsl": true,
    "OAuth2": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "TokenUrl": "https://auth.example.com/oauth/token",
      "Scope": "mcp.read mcp.write"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

#### 配置项说明

**SseClient**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Url` | string | 是 | SSE 端点 URL |
| `AccessToken` | string | 否 | Bearer Token（与 OAuth2 二选一） |
| `Headers` | object | 否 | 自定义 HTTP 头 |
| `VerifySsl` | bool | 否 | 是否验证 SSL 证书，默认 `true` |
| `OAuth2` | object | 否 | OAuth2 配置（与 AccessToken 二选一） |

**OAuth2**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ClientId` | string | 是 | OAuth2 客户端 ID |
| `ClientSecret` | string | 是 | OAuth2 客户端密钥 |
| `TokenUrl` | string | 是 | Token 端点 URL |
| `Scope` | string | 否 | 请求的作用域 |

#### 示例配置

**使用 Bearer Token**

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "AccessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

**使用 OAuth2**

```json
{
  "SseClient": {
    "Url": "https://api.example.com/mcp/sse",
    "OAuth2": {
      "ClientId": "mcp-client",
      "ClientSecret": "secret123",
      "TokenUrl": "https://auth.example.com/oauth/token",
      "Scope": "mcp.api"
    }
  }
}
```

### 2.4 环境变量配置

可以使用环境变量覆盖配置文件：

```bash
# HTTP 服务器配置
export MCPPROXY_HttpServer__Port=8080
export MCPPROXY_HttpServer__Host=0.0.0.0

# MCP 服务器配置
export MCPPROXY_McpServers__0__Name=filesystem
export MCPPROXY_McpServers__0__Command=npx
export MCPPROXY_McpServers__0__Arguments__0=-y
export MCPPROXY_McpServers__0__Arguments__1=@modelcontextprotocol/server-filesystem

# 全局配置
export MCPPROXY_UseNamespacePrefix=true
export MCPPROXY_AllowServerFilter=true

# SSE 客户端配置
export MCPPROXY_SseClient__Url=https://api.example.com/sse
export MCPPROXY_SseClient__AccessToken=your-token

# OAuth2 配置
export MCPPROXY_SseClient__OAuth2__ClientId=xxx
export MCPPROXY_SseClient__OAuth2__ClientSecret=yyy

# 日志级别
export MCPPROXY_Logging__LogLevel__Default=Debug
```

---

## 3. 使用方式

### 3.1 Web API 方式

#### 启动服务

```bash
cd src/McpProxy.StdioToSse.WebApi
dotnet run
```

#### 访问 Swagger UI

打开浏览器: **http://localhost:3000**

#### API 端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/` | GET | Swagger UI 首页 |
| `/health` | GET | 健康检查 |
| `/api/servers` | GET | 获取服务器状态 |
| `/api/capabilities` | GET | 获取聚合能力 |
| `/api/mcp/tools/list` | POST | 列出工具 |
| `/api/mcp/tools/call` | POST | 调用工具 |
| `/api/mcp/prompts/list` | POST | 列出提示 |
| `/api/mcp/prompts/get` | POST | 获取提示 |
| `/api/mcp/resources/list` | POST | 列出资源 |
| `/api/mcp/resources/read` | POST | 读取资源 |

### 3.2 Host Service 方式

#### 启动服务

```bash
cd src/McpProxy.SseToStdio.Host
dotnet run
```

#### 作为系统服务

**Windows**:
```bash
# 发布为单文件
dotnet publish -c Release -r win-x64 --self-contained

# 安装服务
sc.exe create McpProxyService binPath="C:\path\to\McpProxy.SseToStdio.Host.exe"
sc.exe start McpProxyService
```

**Linux (systemd)**:
```bash
# 创建服务文件
sudo nano /etc/systemd/system/mcp-proxy.service

# 启用并启动
sudo systemctl enable mcp-proxy
sudo systemctl start mcp-proxy

# 查看状态
sudo systemctl status mcp-proxy
```

### 3.3 CLI 方式（向后兼容）

```bash
cd src/McpProxy.Cli

# Stdio to SSE 模式
dotnet run stdio-to-sse npx -y @modelcontextprotocol/server-everything \
  --port 3000

# SSE to Stdio 模式
dotnet run sse-to-stdio https://api.example.com/mcp/sse \
  --access-token your-token

# 使用配置文件
dotnet run config appsettings.json
```

---

## 4. API 参考

### 4.1 列出工具

**请求**

```http
POST /api/mcp/tools/list?server=filesystem
Content-Type: application/json
```

**查询参数**
- `server` (可选): 服务器名称，用于过滤特定服务器的工具

**响应**

```json
{
  "tools": [
    {
      "name": "filesystem:read_file",
      "description": "Read the contents of a file",
      "inputSchema": {
        "type": "object",
        "properties": {
          "path": {
            "type": "string",
            "description": "File path"
          }
        },
        "required": ["path"]
      }
    },
    {
      "name": "filesystem:write_file",
      "description": "Write content to a file",
      "inputSchema": {
        "type": "object",
        "properties": {
          "path": { "type": "string" },
          "content": { "type": "string" }
        },
        "required": ["path", "content"]
      }
    }
  ]
}
```

### 4.2 调用工具

**请求**

```http
POST /api/mcp/tools/call
Content-Type: application/json

{
  "name": "filesystem:read_file",
  "arguments": {
    "path": "/etc/hosts"
  }
}
```

**响应**

```json
{
  "content": [
    {
      "type": "text",
      "text": "127.0.0.1 localhost\n::1 localhost\n..."
    }
  ],
  "isError": false
}
```

### 4.3 列出提示

**请求**

```http
POST /api/mcp/prompts/list?server=github
```

**响应**

```json
{
  "prompts": [
    {
      "name": "github:analyze_repo",
      "description": "Analyze a GitHub repository",
      "arguments": [
        {
          "name": "repo",
          "description": "Repository name (owner/repo)",
          "required": true
        }
      ]
    }
  ]
}
```

### 4.4 获取提示

**请求**

```http
POST /api/mcp/prompts/get
Content-Type: application/json

{
  "name": "github:analyze_repo",
  "arguments": {
    "repo": "microsoft/vscode"
  }
}
```

**响应**

```json
{
  "messages": [
    {
      "role": "user",
      "content": {
        "type": "text",
        "text": "Please analyze the repository microsoft/vscode..."
      }
    }
  ]
}
```

### 4.5 列出资源

**请求**

```http
POST /api/mcp/resources/list
```

**响应**

```json
{
  "resources": [
    {
      "uri": "filesystem:file:///home/user/document.txt",
      "name": "document.txt",
      "description": "A text document",
      "mimeType": "text/plain"
    }
  ]
}
```

### 4.6 读取资源

**请求**

```http
POST /api/mcp/resources/read
Content-Type: application/json

{
  "uri": "filesystem:file:///home/user/document.txt"
}
```

**响应**

```json
{
  "contents": [
    {
      "uri": "file:///home/user/document.txt",
      "mimeType": "text/plain",
      "text": "File content here..."
    }
  ]
}
```

### 4.7 获取服务器状态

**请求**

```http
GET /api/servers
```

**响应**

```json
{
  "servers": [
    {
      "name": "filesystem",
      "isConnected": true,
      "serverName": "filesystem-mcp-server",
      "serverVersion": "1.0.0",
      "lastHeartbeat": "2025-12-09T10:30:00Z",
      "capabilities": {
        "tools": {},
        "prompts": {},
        "resources": {}
      }
    },
    {
      "name": "github",
      "isConnected": true,
      "serverName": "github-mcp-server",
      "serverVersion": "1.2.0",
      "lastHeartbeat": "2025-12-09T10:30:05Z",
      "capabilities": {
        "tools": {},
        "prompts": {}
      }
    }
  ],
  "count": 2,
  "timestamp": "2025-12-09T10:30:10Z"
}
```

### 4.8 获取聚合能力

**请求**

```http
GET /api/capabilities
```

**响应**

```json
{
  "tools": {},
  "prompts": {},
  "resources": {}
}
```

---

## 5. 部署指南

### 5.1 Docker 部署

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
EXPOSE 3000
ENTRYPOINT ["dotnet", "McpProxy.StdioToSse.WebApi.dll"]
```

#### 构建镜像

```bash
# 发布应用
dotnet publish -c Release -o publish

# 构建 Docker 镜像
docker build -t mcp-proxy:latest .

# 运行容器
docker run -d \
  -p 3000:3000 \
  -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
  --name mcp-proxy \
  mcp-proxy:latest
```

#### Docker Compose

```yaml
version: '3.8'

services:
  mcp-proxy:
    image: mcp-proxy:latest
    ports:
      - "3000:3000"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

```bash
docker-compose up -d
```

### 5.2 Kubernetes 部署

#### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-proxy
  labels:
    app: mcp-proxy
spec:
  replicas: 3
  selector:
    matchLabels:
      app: mcp-proxy
  template:
    metadata:
      labels:
        app: mcp-proxy
    spec:
      containers:
      - name: mcp-proxy
        image: mcp-proxy:latest
        ports:
        - containerPort: 3000
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: MCPPROXY_HttpServer__Port
          value: "3000"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
```

#### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: mcp-proxy-service
spec:
  type: LoadBalancer
  selector:
    app: mcp-proxy
  ports:
  - port: 80
    targetPort: 3000
    protocol: TCP
```

#### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: mcp-proxy-config
data:
  appsettings.json: |
    {
      "HttpServer": {
        "Port": 3000
      },
      "McpServers": [
        {
          "Name": "default",
          "Command": "npx",
          "Arguments": ["-y", "@modelcontextprotocol/server-everything"]
        }
      ]
    }
```

```bash
# 应用配置
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml

# 查看状态
kubectl get pods
kubectl get svc
```

### 5.3 系统服务部署

#### Windows Service

```bash
# 1. 发布为单文件
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true

# 2. 安装服务
sc.exe create McpProxyService \
  binPath="C:\McpProxy\McpProxy.SseToStdio.Host.exe" \
  DisplayName="MCP Proxy Service" \
  start=auto

# 3. 启动服务
sc.exe start McpProxyService

# 4. 查看状态
sc.exe query McpProxyService

# 5. 停止服务
sc.exe stop McpProxyService

# 6. 删除服务
sc.exe delete McpProxyService
```

#### Linux systemd

```bash
# 1. 创建服务文件
sudo nano /etc/systemd/system/mcp-proxy.service
```

```ini
[Unit]
Description=MCP Proxy Service
After=network.target

[Service]
Type=notify
User=mcpproxy
WorkingDirectory=/opt/mcp-proxy
ExecStart=/opt/mcp-proxy/McpProxy.SseToStdio.Host
Restart=always
RestartSec=10
SyslogIdentifier=mcp-proxy

[Install]
WantedBy=multi-user.target
```

```bash
# 2. 重载 systemd
sudo systemctl daemon-reload

# 3. 启用服务
sudo systemctl enable mcp-proxy

# 4. 启动服务
sudo systemctl start mcp-proxy

# 5. 查看状态
sudo systemctl status mcp-proxy

# 6. 查看日志
sudo journalctl -u mcp-proxy -f
```

---

## 6. 故障排除

### 6.1 常见错误

#### 错误 1: 端口已被占用

```
错误: Failed to bind to address http://localhost:3000: address already in use
```

**解决方案**:
```bash
# 查找占用端口的进程
netstat -ano | findstr :3000  # Windows
lsof -i :3000                  # Linux/macOS

# 修改配置使用其他端口
{
  "HttpServer": {
    "Port": 8080
  }
}
```

#### 错误 2: MCP 服务器启动失败

```
错误: Failed to connect to server 'xxx': Process exited with code 1
```

**解决方案**:
```bash
# 1. 手动测试命令
npx -y @modelcontextprotocol/server-filesystem /

# 2. 检查工作目录
ls /path/to/workdir

# 3. 检查环境变量
echo $NODE_ENV

# 4. 增加日志级别
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

#### 错误 3: OAuth2 认证失败

```
错误: 401 Unauthorized
```

**解决方案**:
```bash
# 1. 手动测试 OAuth2
curl -X POST https://auth.example.com/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=your-client-id" \
  -d "client_secret=your-client-secret"

# 2. 检查配置
{
  "OAuth2": {
    "ClientId": "...",  // 确认正确
    "ClientSecret": "...",  // 确认正确
    "TokenUrl": "...",  // 确认 URL 正确
    "Scope": "..."  // 确认作用域正确
  }
}
```

### 6.2 日志分析

#### 启用详细日志

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  }
}
```

#### 日志位置

| 部署方式 | 日志位置 |
|---------|---------|
| **控制台** | Stdout/Stderr |
| **Docker** | `docker logs <container>` |
| **Kubernetes** | `kubectl logs <pod>` |
| **Windows Service** | 事件查看器 |
| **Linux systemd** | `journalctl -u mcp-proxy` |

#### 常用日志命令

```bash
# 查看实时日志（Docker）
docker logs -f mcp-proxy

# 查看最近 100 行（Kubernetes）
kubectl logs mcp-proxy-xxx --tail=100

# 查看实时日志（systemd）
sudo journalctl -u mcp-proxy -f

# 导出日志到文件
kubectl logs mcp-proxy-xxx > mcp-proxy.log
```

### 6.3 性能问题

#### 症状: API 响应慢

**诊断**:
```bash
# 查看服务器状态
curl http://localhost:3000/api/servers

# 测试单个服务器响应时间
time curl -X POST http://localhost:3000/api/mcp/tools/list?server=filesystem
```

**解决**:
```json
// 1. 启用缓存
{
  "OutputCache": {
    "DefaultExpirationTimeSpan": "00:05:00"
  }
}

// 2. 减少健康检查频率
{
  "McpServers": {
    "HealthCheckInterval": 60
  }
}

// 3. 禁用不必要的服务器
{
  "McpServers": [
    {
      "Name": "slow-server",
      "Enabled": false
    }
  ]
}
```

### 6.4 健康检查

```bash
# 基础健康检查
curl http://localhost:3000/health

# 详细状态
curl http://localhost:3000/api/servers | jq

# 检查特定服务器
curl http://localhost:3000/api/mcp/tools/list?server=filesystem
```

---

## 7. 最佳实践

### 7.1 安全建议

#### 1. 使用 HTTPS

```csharp
// Program.cs
app.UseHttpsRedirection();

// appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "your-password"
        }
      }
    }
  }
}
```

#### 2. 限制 CORS

```json
{
  "HttpServer": {
    "AllowedOrigins": [
      "https://your-app.com",
      "https://another-app.com"
    ]
  }
}
```

#### 3. 使用认证

```csharp
// 添加 JWT 认证
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options => { /* ... */ });

app.UseAuthentication();
app.UseAuthorization();
```

#### 4. 保护敏感配置

```bash
# 使用环境变量
export MCPPROXY_SseClient__AccessToken=<secret>

# 使用 Azure Key Vault
builder.Configuration.AddAzureKeyVault(/* ... */);

# 使用 Kubernetes Secrets
kubectl create secret generic mcp-secrets \
  --from-literal=access-token=<secret>
```

### 7.2 性能优化

#### 1. 启用响应压缩

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

app.UseResponseCompression();
```

#### 2. 使用输出缓存

```csharp
builder.Services.AddOutputCache();

app.MapPost("/api/mcp/tools/list", ...)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
```

#### 3. 限流

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

app.UseRateLimiter();
```

### 7.3 监控和可观测性

#### 1. 健康检查

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<McpServerHealthCheck>("mcp_servers");
```

#### 2. 指标收集

```csharp
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
    });
```

#### 3. 分布式追踪

```csharp
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddJaegerExporter();
    });
```

### 7.4 高可用性

#### 1. 水平扩展

```yaml
# Kubernetes
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
```

#### 2. 健康探针

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 3000
  initialDelaySeconds: 30
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health
    port: 3000
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
```

#### 3. 优雅关闭

```csharp
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    // 停止接受新请求
    // 等待现有请求完成
    Console.WriteLine("Gracefully shutting down...");
});
```

---

## 附录

### A. 配置模板

#### 基础配置

```json
{
  "HttpServer": {
    "Port": 3000,
    "AllowedOrigins": ["*"]
  },
  "McpServers": [
    {
      "Name": "default",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-everything"]
    }
  ]
}
```

#### 生产配置

```json
{
  "HttpServer": {
    "Host": "0.0.0.0",
    "Port": 443,
    "AllowedOrigins": [
      "https://your-app.com"
    ]
  },
  "McpServers": [
    {
      "Name": "fs",
      "Command": "npx",
      "Arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/data"],
      "Enabled": true
    }
  ],
  "UseNamespacePrefix": false,
  "AutoReconnect": true,
  "HealthCheckInterval": 30,
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### B. 故障排除清单

- [ ] 检查端口是否被占用
- [ ] 检查配置文件语法
- [ ] 检查 MCP 服务器命令是否正确
- [ ] 检查环境变量是否设置
- [ ] 检查日志输出
- [ ] 测试健康检查端点
- [ ] 验证网络连接
- [ ] 检查防火墙规则

### C. 常用命令参考

```bash
# 启动服务
dotnet run

# 构建
dotnet build -c Release

# 发布
dotnet publish -c Release -o publish

# 运行测试
dotnet test

# 查看日志
tail -f /var/log/mcp-proxy.log

# 重启服务
sudo systemctl restart mcp-proxy

# 检查端口
netstat -tulpn | grep 3000
```

---

**文档维护者**: MCP Proxy Team  
**技术支持**: GitHub Issues  
**社区**: GitHub Discussions  
**更新频率**: 持续更新
