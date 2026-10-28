// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.UnitTests;

[TestClass]
public class DefaultSqlConnectionBuilderTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string DefaultConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Encrypt=true";

    private readonly SqlRetryLogicBaseProvider _retryProvider = Substitute.For<SqlRetryLogicBaseProvider>();

    [TestMethod]
    public void GivenNullOptions_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultSqlConnectionBuilder(null, _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullOptionsValue_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        IOptions<SqlServerDataStoreOptions> options = Substitute.For<IOptions<SqlServerDataStoreOptions>>();
        options.Value.Returns((SqlServerDataStoreOptions)null);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultSqlConnectionBuilder(options, _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullDatabases_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        IOptions<SqlServerDataStoreOptions> options = Options.Create(new SqlServerDataStoreOptions { Databases = null });

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultSqlConnectionBuilder(options, _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNoDatabases_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        IOptions<SqlServerDataStoreOptions> options = CreateOptions();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultSqlConnectionBuilder(options, _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenMultipleDatabases_WhenCreatingBuilder_ThenThrowArgumentException()
    {
        IOptions<SqlServerDataStoreOptions> options = CreateOptions(
            ("first", DefaultConnectionString),
            ("second", $"server={ServerName};Initial Catalog=Other"));

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => new DefaultSqlConnectionBuilder(options, _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullRetryProvider_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        IOptions<SqlServerDataStoreOptions> options = CreateOptions(("default", DefaultConnectionString));

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new DefaultSqlConnectionBuilder(options, null));
        Assert.AreEqual("retryProvider", ex.ParamName);
    }

    [TestMethod]
    public void GivenSingleDatabase_WhenGettingDefaultDatabase_ThenReturnInitialCatalog()
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", DefaultConnectionString)), _retryProvider);

        Assert.AreEqual(DatabaseName, builder.DefaultDatabase);
    }

    [TestMethod]
    public void GivenConnectionStringWithoutInitialCatalog_WhenGettingDefaultDatabase_ThenReturnEmptyString()
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", $"server={ServerName}")), _retryProvider);

        Assert.AreEqual(string.Empty, builder.DefaultDatabase);
    }

    [TestMethod]
    public void GivenDefaultSettings_WhenCreatingConnection_ThenReturnConfiguredConnection()
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", DefaultConnectionString)), _retryProvider);

        using SqlConnection connection = builder.CreateConnection();

        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    [DataRow(DatabaseName)]
    [DataRow(MasterDatabase)]
    [DataRow("fhir")]
    public void GivenInitialCatalogOverride_WhenCreatingConnection_ThenReturnModifiedConnection(string initialCatalog)
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", DefaultConnectionString)), _retryProvider);

        using SqlConnection connection = builder.CreateConnection(b => b.InitialCatalog = initialCatalog);

        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(initialCatalog, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
        Assert.AreEqual(DatabaseName, builder.DefaultDatabase);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(100)]
    public void GivenMaxPoolSizeOverride_WhenCreatingConnection_ThenReturnModifiedConnection(int maxPoolSize)
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", DefaultConnectionString)), _retryProvider);

        using SqlConnection connection = builder.CreateConnection(b => b.MaxPoolSize = maxPoolSize);

        Assert.AreEqual(ServerName, connection.DataSource);
        Assert.AreEqual(DatabaseName, connection.Database);
        Assert.AreEqual(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    public void GivenPreviousConfiguration_WhenCreatingAnotherConnection_ThenDoNotReusePreviousChanges()
    {
        var builder = new DefaultSqlConnectionBuilder(CreateOptions(("default", DefaultConnectionString)), _retryProvider);

        using SqlConnection first = builder.CreateConnection(b => b.InitialCatalog = MasterDatabase);
        using SqlConnection second = builder.CreateConnection();

        Assert.AreNotSame(first, second);
        Assert.AreEqual(MasterDatabase, first.Database);
        Assert.AreEqual(DatabaseName, second.Database);
    }

    private static IOptions<SqlServerDataStoreOptions> CreateOptions(params (string Key, string ConnectionString)[] databases)
    {
        var map = new Dictionary<string, SqlServerDatabaseOptions>();
        foreach ((string key, string connectionString) in databases)
        {
            map.Add(key, new SqlServerDatabaseOptions { ConnectionString = connectionString });
        }

        return Options.Create(new SqlServerDataStoreOptions { Databases = map });
    }
}
