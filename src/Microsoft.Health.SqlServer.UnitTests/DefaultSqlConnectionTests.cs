// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features;

[TestClass]
public class DefaultSqlConnectionTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string DefaultConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Encrypt=true";

    private readonly SqlRetryLogicBaseProvider _retryProvider = Substitute.For<SqlRetryLogicBaseProvider>();

    [TestMethod]
    public void GivenDefaultSettings_WhenSqlConnectionRequested_ThenReturnSameValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection();
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    public async Task GivenDefaultSettings_WhenSqlConnectionAsyncRequested_ThenReturnSameValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync();
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    public async Task GivenDefaultSettings_WhenSqlConnectionAsyncWithAppNameRequested_ThenReturnSameValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(false, "test");
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [DataRow(DatabaseName)]
    [DataRow(MasterDatabase)]
    [DataRow("fhir")]
    public void GivenInitialCatalogOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(string initialCatalog)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(initialCatalog);
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(initialCatalog, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [DataRow(DatabaseName)]
    [DataRow(MasterDatabase)]
    [DataRow("fhir")]
    public async Task GivenInitialCatalogOverride_WhenSqlConnectionAsyncRequested_ThenReturnModifiedValue(string initialCatalog)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(initialCatalog);
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(initialCatalog, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(100)]
    public void GivenMaxPoolOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(int maxPoolSize)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(maxPoolSize: maxPoolSize);
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreEqual(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(100)]
    public async Task GivenMaxPoolOverride_WhenSqlConnectionAsyncRequested_ThenReturnModifiedValue(int maxPoolSize)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync(maxPoolSize: maxPoolSize);
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreEqual(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [Obsolete("Test should be removed when AuthenticationType is removed.")]
    public void GivenManagedIdentity_WhenSqlConnectionRequested_ThenReturnModifiedValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration
        {
            AuthenticationType = SqlServerAuthenticationType.ManagedIdentity,
            ConnectionString = DefaultConnectionString,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
        });

        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection();
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);

        var actual = new SqlConnectionStringBuilder(connection.ConnectionString);
        Assert.AreEqual(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, actual.Authentication);
        Assert.AreEqual(options.Value.ManagedIdentityClientId, actual.UserID);
    }

    [TestMethod]
    [Obsolete("Test should be removed when AuthenticationType is removed.")]
    public async Task GivenManagedIdentity_WhenSqlConnectionAsyncRequested_ThenReturnModifiedValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration
        {
            AuthenticationType = SqlServerAuthenticationType.ManagedIdentity,
            ConnectionString = DefaultConnectionString,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
        });

        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.AreEqual(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = await connectionBuilder.GetSqlConnectionAsync();
        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);

        var actual = new SqlConnectionStringBuilder(connection.ConnectionString);
        Assert.AreEqual(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, actual.Authentication);
        Assert.AreEqual(options.Value.ManagedIdentityClientId, actual.UserID);
    }
}
