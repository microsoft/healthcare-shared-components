// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Functions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Functions.Tests.Integration;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used by test framework.")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by test framework.")]
public class class WebJobsTestFixture<T> : IAsyncLifetime where T : FunctionsStartup, new()
{
    private readonly IHost _jobHost;

    public WebJobsTestFixture(IMessageSink sink)
        => _jobHost = AzureFunctionsJobHostBuilder
            .Create<T>()
            .ConfigureLogging(b => b.AddXUnit(sink))
            .ConfigureWebJobs(b => b.AddDurableTask())
            .Build();

    public Task DisposeAsync()
        => _jobHost.StopAsync();

    public Task InitializeAsync()
        => _jobHost.StartAsync();
}
