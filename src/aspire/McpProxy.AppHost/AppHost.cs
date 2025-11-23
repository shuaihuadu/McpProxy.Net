var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.McpProxy_AppServer>("mcpproxy-appserver");

builder.Build().Run();
