// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Functions.Extensions;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests;

public class IFunctionsHostBuilderExtensionsTests
{
    [Fact]
    public void GivenAzureFunctionsHost_WhenGettingHostConfig_ThenGetCorrectSection()
    {
        const string SectionName = "Options";
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                KeyValuePair.Create($"{nameof(TestOptions.Word)}", "foo"),
                KeyValuePair.Create($"{AzureFunctionsJobHost.RootSectionName}:{nameof(TestOptions.Word)}", "bar"),
                KeyValuePair.Create($"{AzureFunctionsJobHost.RootSectionName}:{SectionName}:{nameof(TestOptions.Word)}", "baz"),
                KeyValuePair.Create($"{AzureFunctionsJobHost.RootSectionName}:{SectionName}:{nameof(TestOptions.Number)}", "42"),
            }!)
            .Build();

        IConfigurationSection? hostConfig = CreateBuilder(config).GetHostConfiguration() as IConfigurationSection;

        Assert.NotNull(hostConfig);
        Assert.Equal(AzureFunctionsJobHost.RootSectionName, hostConfig!.Path);

        TestOptions actual = new TestOptions();
        hostConfig.GetSection(SectionName).Bind(actual);

        Assert.Equal(42, actual.Number);
        Assert.Equal("baz", actual.Word);
    }

    private static IFunctionsHostBuilder CreateBuilder(IConfiguration config)
    {
        // GetContext() is an extension method with an implementation that is not very conducive for testing
        Type t = typeof(IFunctionsHostBuilder).Assembly.GetTypes().Single(x => x.FullName == "Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsHostBuilder")!;
        return (IFunctionsHostBuilder)Activator.CreateInstance(t, new ServiceCollection(), new WebJobsBuilderContext { Configuration = config })!;
    }

    private sealed class TestOptions
    {
        public int Number { get; set; }

        public string? Word { get; set; }
    }
}
