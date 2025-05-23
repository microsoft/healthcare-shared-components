// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This type is loaded via reflection.")]
internal sealed class TestModule : IStartupModule
{
    public void Load(IServiceCollection services)
    {
        services.AddScoped<TestComponent>();
    }
}
