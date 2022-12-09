// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Functions.Extensions.Configuration;
using NSubstitute;

namespace Microsoft.Health.Functions.Extensions;

/// <summary>
/// Represents a builder for instances of <see cref="IHost"/> that run Azure Functions.
/// </summary>
public sealed class AzureFunctionsJobHostBuilder
{
    private readonly string _root;
    private readonly IHostBuilder _hostBuilder = new HostBuilder();

    private Action<HostBuilderContext, IWebJobsBuilder, Action<HostBuilderContext, ILoggingBuilder>> _configureWebJobs;
    private Action<HostBuilderContext, ILoggingBuilder> _configureLogger = BeginConfigureLogging;

    private AzureFunctionsJobHostBuilder(string root, Action<HostBuilderContext, IWebJobsBuilder, Action<HostBuilderContext, ILoggingBuilder>> configure)
    {
        _root = EnsureArg.IsNotNull(root, nameof(root));
        _configureWebJobs = EnsureArg.IsNotNull(configure, nameof(configure));
    }

    /// <summary>
    /// Configures the logging used by the Azure Functions host.
    /// </summary>
    /// <param name="configure">A delegate for configuring logging.</param>
    /// <returns>The <see cref="AzureFunctionsJobHostBuilder"/> instance.</returns>
    public AzureFunctionsJobHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _configureLogger += (c, b) => configure(b);
        return this;
    }

    /// <summary>
    /// Configures the Azure Functions host, such as adding new extensions.
    /// </summary>
    /// <param name="configure">A delegate for configuring the host.</param>
    /// <returns>The <see cref="AzureFunctionsJobHostBuilder"/> instance.</returns>
    public AzureFunctionsJobHostBuilder ConfigureWebJobs(Action<IWebJobsBuilder> configure)
    {
        _configureWebJobs += (c, b, l) => configure(b);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="IHost"/> for Azure Functions.
    /// </summary>
    /// <returns>The <see cref="IHost"/> instance.</returns>
    public IHost Build()
        => _hostBuilder
            .UseContentRoot(_root)
            .ConfigureWebJobs(
                (c, b) => _configureWebJobs(c, b, _configureLogger),
                o => { },
                (c, b) =>
                {
                    // Clear the appsettings config source, as in practice we are not in control of appsettings
                    b.ConfigurationBuilder.Sources.Clear();
                    b.ConfigurationBuilder
                        .Add(CreateRootConfigurationSource())
                        .Add(new HostJsonFileConfigurationSource(_root))
                        .Add(new LocalSettingsJsonFileConfigurationSource(_root))
                        .AddEnvironmentVariables();
                })
            .ConfigureLogging((c, b) => _configureLogger(c, b))
            .ConfigureServices(services =>
            {
                services.AddSingleton<TelemetryClient>(new TelemetryClient(new TelemetryConfiguration()
                {
                    TelemetryChannel = Substitute.For<ITelemetryChannel>(),
                }));
            })
            .Build();

    /// <summary>
    /// Creates a new builder that may be used to configure the <see cref="IHost"/>.
    /// </summary>
    /// <typeparam name="T">The type of the custom <see cref="FunctionsStartup"/>.</typeparam>
    /// <returns>An instance <see cref="AzureFunctionsJobHostBuilder"/> that can be additionally configured.</returns>
    public static AzureFunctionsJobHostBuilder Create<T>() where T : FunctionsStartup, new()
        => Create(
            Path.GetDirectoryName(typeof(T).Assembly.Location)!,
            (context, webJobsBuilder, configureLogging) => webJobsBuilder.UseWebJobsStartup(
                typeof(T),
                new WebJobsBuilderContext { Configuration = context.Configuration },
                LoggerFactory.Create(b => configureLogging(context, b))));

    /// <summary>
    /// Creates a new builder that may be used to configure the <see cref="IHost"/>.
    /// </summary>
    /// <remarks>
    /// The content root for the host is derived from the executing assembly.
    /// </remarks>
    /// <returns>An instance <see cref="AzureFunctionsJobHostBuilder"/> that can be additionally configured.</returns>
    public static AzureFunctionsJobHostBuilder Create()
        => Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

    /// <summary>
    /// Creates a new builder that may be used to configure the <see cref="IHost"/>.
    /// </summary>
    /// <param name="root">The content root containing the host's configuration files.</param>
    /// <returns>An instance <see cref="AzureFunctionsJobHostBuilder"/> that can be additionally configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public static AzureFunctionsJobHostBuilder Create(string root)
        => Create(root, (h, w, c) => { });

    private static AzureFunctionsJobHostBuilder Create(string root, Action<HostBuilderContext, IWebJobsBuilder, Action<HostBuilderContext, ILoggingBuilder>> configure)
        => new AzureFunctionsJobHostBuilder(root, configure);

    private static IConfigurationSource CreateRootConfigurationSource()
        => new MemoryConfigurationSource
        {
            InitialData = new KeyValuePair<string, string?>[] { KeyValuePair.Create<string, string?>("AzureWebJobsConfigurationSection", AzureFunctionsJobHost.RootSectionName) },
        };

    private static void BeginConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddConfiguration(context
            .Configuration
            .GetSection(AzureFunctionsJobHost.RootSectionName)
            .GetSection("Logging"));
    }

}
