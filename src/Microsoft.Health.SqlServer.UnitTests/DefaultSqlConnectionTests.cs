// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features;

public class DefaultSqlConnectionTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string DefaultConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Encrypt=true";

    private readonly SqlRetryLogicBaseProvider _retryProvider = Substitute.For<SqlRetryLogicBaseProvider>();

    [Fact]
    public void GivenDefaultSettings_WhenSqlConnectionRequested_ThenReturnSameValue()
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection();
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Fact]
    public async Task GivenDefaultSettings_WhenSqlConnectionAsyncRequested_ThenReturnSameValue()
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync();
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Fact]
    public async Task GivenDefaultSettings_WhenSqlConnectionAsyncWithAppNameRequested_ThenReturnSameValue()
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(false, "test");
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(DatabaseName)]
    [InlineData(MasterDatabase)]
    [InlineData("fhir")]
    public void GivenInitialCatalogOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(string initialCatalog)
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(initialCatalog);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(initialCatalog, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(DatabaseName)]
    [InlineData(MasterDatabase)]
    [InlineData("fhir")]
    public async Task GivenInitialCatalogOverride_WhenSqlConnectionAsyncRequested_ThenReturnModifiedValue(string initialCatalog)
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(initialCatalog);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(initialCatalog, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GivenMaxPoolOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(int maxPoolSize)
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(maxPoolSize: maxPoolSize);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Equal(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task GivenMaxPoolOverride_WhenSqlConnectionAsyncRequested_ThenReturnModifiedValue(int maxPoolSize)
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });

        var connectionBuilder = new DefaultSqlConnectionBuilder(monitor, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(maxPoolSize: maxPoolSize);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Equal(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }
}
