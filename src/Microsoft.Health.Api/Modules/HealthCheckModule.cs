﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Api.Modules;

public class HealthCheckModule : IStartupModule
{
    public void Load(IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.Add<HealthCheckCachingPostConfigure>()
            .Transient()
            .AsService<IPostConfigureOptions<HealthCheckServiceOptions>>();
    }
}
