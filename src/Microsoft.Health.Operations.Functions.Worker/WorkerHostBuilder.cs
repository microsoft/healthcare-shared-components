// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Operations.Functions.Worker;

public abstract class WorkerHostBuilder : IHostBuilder
{
    protected WorkerHostBuilder()
    {
        string contentRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        HostBuilder = new HostBuilder().UseContentRoot(contentRoot);
    }

    protected IHostBuilder HostBuilder { get; private set; }

    public IDictionary<object, object> Properties => HostBuilder.Properties;

    public IHost Build()
        => HostBuilder.Build();

    public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        => HostBuilder.ConfigureAppConfiguration(configureDelegate);

    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        => HostBuilder.ConfigureContainer(configureDelegate);

    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        => HostBuilder.ConfigureHostConfiguration(configureDelegate);

    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        => HostBuilder.ConfigureServices(configureDelegate);

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        => HostBuilder.UseServiceProviderFactory(factory);

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        => HostBuilder.UseServiceProviderFactory(factory);
}
