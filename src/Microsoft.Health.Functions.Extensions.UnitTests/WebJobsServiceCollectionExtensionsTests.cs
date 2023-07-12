// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Microsoft.Health.Functions.Extensions.UnitTests;

public class WebJobsServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenServiceContainer_WhenAddingWebJobsHealthChecks_ThenOnlyFindHealthCheckService()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddWebJobsHealthChecks();

        Assert.Single(services);
        Assert.Equal(typeof(HealthCheckService), services.Single().ServiceType);
    }
}
