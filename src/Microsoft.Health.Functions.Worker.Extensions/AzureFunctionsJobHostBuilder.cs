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
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Functions.Worker.Extensions.Configuration;

namespace Microsoft.Health.Functions.Worker.Extensions;

/// <summary>
/// Represents a builder for instances of <see cref="IHost"/> that run Azure Functions.
/// </summary>
public sealed class AzureFunctionsJobHostBuilder
{
    private readonly string _root;
    private readonly IHostBuilder _hostBuilder = new HostBuilder();

    private Action<HostBuilderContext, IWebJobsBuilder, Action<HostBuilderContext, ILoggingBuilder>> _configureWebJobs = (h, w, c) => { };
    private Action<HostBuilderContext, ILoggingBuilder> _configureLogger = BeginConfigureLogging;
    private readonly List<KeyValuePair<string, string?>> _environmentVariables = new();

    private AzureFunctionsJobHostBuilder(string root)
        => _root = EnsureArg.IsNotNull(root, nameof(root));

    /// <summary>
    /// Configures the Azure Functions host with the specified environment variables.
    /// </summary>
    /// <param name="variables">a collection of environment variables.</param>
    /// <returns>The <see cref="AzureFunctionsJobHostBuilder"/> instance.</returns>
    public AzureFunctionsJobHostBuilder ConfigureEnvironmentVariables(params (string Key, string? Value)[] variables)
    {
        EnsureArg.IsNotNull(variables, nameof(variables));

        foreach ((string key, string? value) in variables)
            _environmentVariables.Add(KeyValuePair.Create(key, value));

        return this;
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
    {
        return _hostBuilder
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
                        .AddEnvironmentVariables()
                        .Add(new MemoryConfigurationSource { InitialData = _environmentVariables });
                })
            .ConfigureLogging((c, b) => _configureLogger(c, b))
            .ConfigureServices((cxt, services) =>
                services
                    .AddSingleton<ITelemetryChannel, NullTelemetryChannel>()
                    .AddSingleton(sp => new TelemetryConfiguration { TelemetryChannel = sp.GetRequiredService<ITelemetryChannel>() })
                    .AddSingleton(sp => new TelemetryClient(sp.GetRequiredService<TelemetryConfiguration>())))
            .Build();
    }

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
        => new AzureFunctionsJobHostBuilder(root);

    private static MemoryConfigurationSource CreateRootConfigurationSource()
    {
        return new MemoryConfigurationSource
        {
            InitialData = new KeyValuePair<string, string?>[] { KeyValuePair.Create<string, string?>("AzureWebJobsConfigurationSection", AzureFunctionsJobHost.RootSectionName) },
        };
    }

    private static void BeginConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddConfiguration(context
            .Configuration
            .GetSection(AzureFunctionsJobHost.RootSectionName)
            .GetSection("Logging"));
    }

    private sealed class NullTelemetryChannel : ITelemetryChannel
    {
        public bool? DeveloperMode { get; set; }

        public string? EndpointAddress { get; set; }

        public void Dispose()
        { }

        public void Flush()
        { }

        public void Send(ITelemetry item)
        { }
    }
}
