// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Functions.Extensions;
using Xunit;

namespace Microsoft.Health.Functions.Tests.Integration;

public class WebJobsTestFixture<T> : IAsyncLifetime
    where T : FunctionsStartup, new()
{
    private readonly IHost _jobHost = AzureFunctionsJobHost.Create<T>((c, b) => b.AddDurableTask());

    public Task DisposeAsync()
        => _jobHost.StopAsync();

    public Task InitializeAsync()
        => _jobHost.StartAsync();
}
