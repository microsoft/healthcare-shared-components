// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Functions.Extensions.Configuration;

namespace Microsoft.Health.Functions.Extensions;

/// <summary>
/// A <see langword="static"/> class for utilities for interacting with the Azure Functions host.
/// </summary>
public static class AzureFunctionsJobHost
{
    /// <summary>
    /// The name of the configuration section in which all user-specified configurations reside.
    /// </summary>
    public const string SectionName = "AzureFunctionsJobHost";

    /// <summary>
    /// Creates an <see cref="IHost"/> instance that encapsulates the Azure Functions job host
    /// whose start-up type is given by <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="FunctionsStartup"/>.</typeparam>
    /// <param name="configure">An optional delegate for configuring the host further, like adding extensions.</param>
    /// <returns>An <see cref="IHost"/> that contains the <see cref="IJobHost"/>.</returns>
    public static IHost Create<T>(Action<HostBuilderContext, IWebJobsBuilder>? configure = null)
        where T : FunctionsStartup, new()
        => CreateBuilder(
                Path.GetDirectoryName(typeof(T).Assembly.Location)!,
                (c, b) =>
                {
                    b.UseWebJobsStartup(
                        typeof(T),
                        new WebJobsBuilderContext { Configuration = c.Configuration },
                        LoggerFactory.Create(c => c.AddConsole()));

                    configure?.Invoke(c, b);
                })
            .Build();

    /// <summary>
    /// Creates an <see cref="IHost"/> instance that encapsulates the Azure Functions job host with no custom start-up class.
    /// </summary>
    /// <param name="configure">An optional delegate for configuring the host further, like adding extensions.</param>
    /// <returns>An <see cref="IHost"/> that contains the <see cref="IJobHost"/>.</returns>
    public static IHost Create(Action<HostBuilderContext, IWebJobsBuilder>? configure = null)
        => Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, configure);

    /// <summary>
    /// Creates an <see cref="IHost"/> instance that encapsulates the Azure Functions job host with no custom start-up class.
    /// </summary>
    /// <param name="root">The root directory containing the various host configurations.</param>
    /// <param name="configure">An optional delegate for configuring the host further, like adding extensions.</param>
    /// <returns>An <see cref="IHost"/> that contains the <see cref="IJobHost"/>.</returns>
    public static IHost Create(string root, Action<HostBuilderContext, IWebJobsBuilder>? configure = null)
        => CreateBuilder(root, (b, c) => configure?.Invoke(b, c)).Build();

    private static IHostBuilder CreateBuilder(string root, Action<HostBuilderContext, IWebJobsBuilder> configure)
    {
        EnsureArg.IsNotNull(root, nameof(root));

        return new HostBuilder()
            .UseContentRoot(root)
            .ConfigureLogging(l => l.AddConsole())
            .ConfigureWebJobs(
                configure,
                o => { },
                (c, b) =>
                {
                    // Clear the appsettings config source, as in practice we are not in control of appsettings
                    b.ConfigurationBuilder.Sources.Clear();
                    b.ConfigurationBuilder
                        .Add(CreateRootConfigurationSource())
                        .Add(new HostJsonFileConfigurationSource(root))
                        .Add(new LocalSettingsJsonFileConfigurationSource(root))
                        .AddEnvironmentVariables();
                });
    }

    private static IConfigurationSource CreateRootConfigurationSource()
        => new MemoryConfigurationSource
        {
            InitialData = new KeyValuePair<string, string>[] { KeyValuePair.Create("AzureWebJobsConfigurationSection", SectionName) },
        };
}
