// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Api.Modules;

/// <summary>
/// Ensures that all <see cref="IRequireInitializationOnFirstRequest"/> instances are
/// initialized before any controllers are invoked.
/// </summary>
public class InitializationModule : IStartupModule, IStartupFilter
{
    /// <inheritdoc />
    public void Load(IServiceCollection services)
    {
        services.AddSingleton<IStartupFilter>(this);
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            Configure(builder);
            next(builder);
        };
    }

    private static void Configure(IApplicationBuilder app)
    {
        // If there are any IRequireInitializationOnFirstRequest services, ensure they are initialized on the first request.

        IRequireInitializationOnFirstRequest[] requireInitializationsOnFirstRequest = app.ApplicationServices.GetService<IEnumerable<IRequireInitializationOnFirstRequest>>().ToArray();
        if (requireInitializationsOnFirstRequest.Length == 0)
        {
            return;
        }

        // Register a middleware component that will be called on every request,
        // ensuring that all components are initialized before a controller
        // handles the request.

        bool initializationComplete = false;
        app.Use(async (httpContext, next) =>
        {
            if (!initializationComplete)
            {
                foreach (var initializable in requireInitializationsOnFirstRequest)
                {
                    await initializable.EnsureInitialized().ConfigureAwait(false);
                }

                initializationComplete = true;
            }

            await next().ConfigureAwait(false);
        });
    }
}
