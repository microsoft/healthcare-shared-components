// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        Assert.Equal(1, services.Count);
        Assert.Equal(typeof(HealthCheckService), services.Single().ServiceType);
    }
}
