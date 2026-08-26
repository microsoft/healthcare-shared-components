// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

public sealed class SchemaInitializerTests
{
    [Theory]
    [InlineData(null, 5, (int)SecondarySchemaStatus.Unknown)]
    [InlineData(3, 5, (int)SecondarySchemaStatus.Behind)]
    [InlineData(1, 2, (int)SecondarySchemaStatus.Behind)]
    [InlineData(5, 5, (int)SecondarySchemaStatus.Current)]
    [InlineData(7, 5, (int)SecondarySchemaStatus.Current)]
    public void GivenSchemaVersions_WhenGettingSecondarySchemaStatus_ReturnsExpectedStatus(int? currentVersion, int maximumSupportedVersion, int expectedStatus)
    {
        Assert.Equal(expectedStatus, (int)SchemaInitializer.GetSecondarySchemaStatus(currentVersion, maximumSupportedVersion));
    }

    [Fact]
    public async Task GivenWriteGateReturnsFalse_WhenCheckingCanApplySchemaUpdates_ReturnsFalseAndConsultsGate()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));
        using ServiceProvider provider = BuildScopedProvider(gate);
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaInitializer>.Instance);

        bool result = await initializer.CanApplySchemaUpdatesAsync(provider, CancellationToken.None);

        Assert.False(result);
        await gate.ReceivedWithAnyArgs(1).CanWriteAsync(default);
    }

    [Fact]
    public async Task GivenWriteGateReturnsTrue_WhenCheckingCanApplySchemaUpdates_ReturnsTrue()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        using ServiceProvider provider = BuildScopedProvider(gate);
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaInitializer>.Instance);

        bool result = await initializer.CanApplySchemaUpdatesAsync(provider, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task GivenWriteGateReturnsFalseAndSchemaBehind_WhenCheckingCanApplySchemaUpdates_LogsBehind()
    {
        var logger = new ListLogger<SchemaInitializer>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, logger);

        await initializer.CanApplySchemaUpdatesAsync(BuildScopedProvider(FalseGate()), CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("is behind", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenWriteGateReturnsFalseAndSchemaCurrent_WhenCheckingCanApplySchemaUpdates_LogsCurrent()
    {
        var logger = new ListLogger<SchemaInitializer>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 5 }, logger);

        await initializer.CanApplySchemaUpdatesAsync(BuildScopedProvider(FalseGate()), CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("is current", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenWriteGateReturnsFalseAndVersionUnknown_WhenCheckingCanApplySchemaUpdates_LogsWarning()
    {
        var logger = new ListLogger<SchemaInitializer>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = null }, logger);

        await initializer.CanApplySchemaUpdatesAsync(BuildScopedProvider(FalseGate()), CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("could not be determined", entry.Message, StringComparison.Ordinal);
    }

    private static ISchemaWriteGate FalseGate()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));
        return gate;
    }

    private static ServiceProvider BuildScopedProvider(ISchemaWriteGate gate)
    {
        var services = new ServiceCollection();
        services.AddSingleton(gate);
        return services.BuildServiceProvider();
    }

    private static SchemaInitializer CreateInitializer(SchemaInformation schemaInformation, ILogger<SchemaInitializer> logger)
    {
        return new SchemaInitializer(
            Substitute.For<IServiceProvider>(),
            Options.Create(new SqlServerDataStoreConfiguration()),
            schemaInformation,
            Substitute.For<IMediator>(),
            logger);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new List<(LogLevel Level, string Message)>();

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
