// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace McpProxy.Core.UnitTests;

public class McpRuntimeTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        return services.BuildServiceProvider();
    }
}
