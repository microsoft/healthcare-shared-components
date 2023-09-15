// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Functions.Extensions;

namespace Microsoft.Health.Operations.Functions;

/// <summary>
/// A <see langword="static"/> class with functions host utilities that can be used on start up.
/// </summary>
public static class IFunctionsHostBuilderExtensions
{
    /// <summary>
    /// Gets the user configuration from the <see cref="IFunctionsHostBuilder"/> when configuring services.
    /// </summary>
    /// <param name="functionsHostBuilder">The host builder used during dependency injection for functions.</param>
    /// <returns>The corresponding <see cref="IConfiguration"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functionsHostBuilder"/> is <see langword="null"/>.</exception>
    public static IConfiguration GetHostConfiguration(this IFunctionsHostBuilder functionsHostBuilder)
        => EnsureArg
            .IsNotNull(functionsHostBuilder)
            .GetContext()
            .Configuration
            .GetSection(AzureFunctionsJobHost.RootSectionName);
}
