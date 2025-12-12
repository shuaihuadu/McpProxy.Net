# 📁 McpProxy.Cli 配置文件快速参考

## 目录结构

```
McpProxy.Cli/
├── appsettings.json                      ← 默认配置（仅日志）
└── config-examples/                      ← 示例配置目录 📚
    ├── README.md                         ← 详细使用说明
    ├── GITIGNORE_RULES.md               ← Git 配置建议
    ├── basic.example.json               ← 基础配置
    ├── sse-to-stdio.example.json        ← SSE→Stdio
    ├── stdio-to-http.example.json       ← Stdio→HTTP
    ├── multi-stdio.example.json         ← 多服务器
    ├── oauth2.example.json              ← OAuth2 认证
    └── test.example.json                ← 测试配置
```

## 快速使用

### 💡 推荐：命令行参数

```bash
# SSE→Stdio
mcp-proxy sse-to-stdio https://api.example.com/sse --access-token "token"

# Stdio→HTTP
mcp-proxy stdio-to-sse npx -y server --port 8080
```

### 📄 备选：配置文件

```bash
# 1. 复制示例
cp config-examples/sse-to-stdio.example.json my-config.json

# 2. 编辑配置
notepad my-config.json

# 3. 运行
mcp-proxy config my-config.json
```

## 示例配置选择

| 场景 | 文件 | 命令 |
|------|------|------|
| 🌐 连接远程 SSE | `sse-to-stdio.example.json` | `mcp-proxy sse-to-stdio <url>` |
| 🚀 启动 HTTP 服务 | `stdio-to-http.example.json` | `mcp-proxy stdio-to-sse <cmd>` |
| 🔀 多服务器管理 | `multi-stdio.example.json` | `mcp-proxy config multi.json` |
| 🔐 OAuth2 认证 | `oauth2.example.json` | `mcp-proxy sse-to-stdio --client-id...` |

## 安全提示

### ✅ 提交到 Git
- `appsettings.json` - 默认配置
- `config-examples/*.json` - 示例配置

### ❌ 不要提交
- `production.json` - 生产配置
- `*.local.json` - 本地配置
- 任何包含敏感信息的文件

## .gitignore 配置

```gitignore
# 添加到项目根目录的 .gitignore
src/McpProxy.Cli/*.json
!src/McpProxy.Cli/appsettings.json
!src/McpProxy.Cli/config-examples/*.json
```

## 常用操作

```bash
# 查看帮助
mcp-proxy --help

# 查看示例说明
cat config-examples/README.md

# 列出所有示例
ls config-examples/*.json

# 运行测试配置
mcp-proxy config config-examples/test.example.json
```

## 📚 完整文档

- **详细说明**: `config-examples/README.md`
- **Git 配置**: `config-examples/GITIGNORE_RULES.md`
- **整理总结**: `docs/CONFIG_FILES_CLEANUP.md`

---

**更新**: 2025-12-09 | **版本**: v1.0.0 | **状态**: ✅
