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
public class ScopedSqlConnectionBuilderTests
{
    private const string FirstKey = "tenant1";
    private const string FirstServer = "server1";
    private const string FirstDatabase = "Database1";
    private const string SecondKey = "tenant2";
    private const string SecondServer = "server2";
    private const string SecondDatabase = "Database2";

    private readonly SqlRetryLogicBaseProvider _retryProvider = Substitute.For<SqlRetryLogicBaseProvider>();
    private readonly IOptionsSnapshot<SqlServerDataStoreOptions> _options = Substitute.For<IOptionsSnapshot<SqlServerDataStoreOptions>>();

    public ScopedSqlConnectionBuilderTests()
    {
        _options.Value.Returns(new SqlServerDataStoreOptions
        {
            Databases = new Dictionary<string, SqlServerDatabaseOptions>
            {
                [FirstKey] = new SqlServerDatabaseOptions { ConnectionString = $"server={FirstServer};Initial Catalog={FirstDatabase}" },
                [SecondKey] = new SqlServerDatabaseOptions { ConnectionString = $"server={SecondServer};Initial Catalog={SecondDatabase}" },
            },
        });
    }

    [TestMethod]
    public void GivenNullOptions_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new ScopedSqlConnectionBuilder(null, CreateContext(FirstKey), _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullOptionsValue_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        _options.Value.Returns((SqlServerDataStoreOptions)null);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new ScopedSqlConnectionBuilder(_options, CreateContext(FirstKey), _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullDatabases_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        _options.Value.Returns(new SqlServerDataStoreOptions { Databases = null });

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new ScopedSqlConnectionBuilder(_options, CreateContext(FirstKey), _retryProvider));
        Assert.AreEqual("options", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullContext_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new ScopedSqlConnectionBuilder(_options, null, _retryProvider));
        Assert.AreEqual("context", ex.ParamName);
    }

    [TestMethod]
    public void GivenNullRetryProvider_WhenCreatingBuilder_ThenThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => new ScopedSqlConnectionBuilder(_options, CreateContext(FirstKey), null));
        Assert.AreEqual("retryProvider", ex.ParamName);
    }

    [TestMethod]
    public void GivenUnknownKey_WhenCreatingBuilder_ThenThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => new ScopedSqlConnectionBuilder(_options, CreateContext("unknown"), _retryProvider));
        Assert.AreEqual("context", ex.ParamName);
        Assert.Contains("'unknown'", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    [DataRow(FirstKey, FirstDatabase)]
    [DataRow(SecondKey, SecondDatabase)]
    public void GivenKnownKey_WhenGettingDefaultDatabase_ThenReturnMatchingInitialCatalog(string key, string expectedDatabase)
    {
        var builder = new ScopedSqlConnectionBuilder(_options, CreateContext(key), _retryProvider);

        Assert.AreEqual(expectedDatabase, builder.DefaultDatabase);
    }

    [TestMethod]
    [DataRow(FirstKey, FirstServer, FirstDatabase)]
    [DataRow(SecondKey, SecondServer, SecondDatabase)]
    public void GivenKnownKey_WhenCreatingConnection_ThenReturnConnectionForMatchingDatabase(string key, string expectedServer, string expectedDatabase)
    {
        var builder = new ScopedSqlConnectionBuilder(_options, CreateContext(key), _retryProvider);

        using SqlConnection connection = builder.CreateConnection();

        Assert.AreEqual(expectedServer, connection.DataSource);
        Assert.AreEqual(expectedDatabase, connection.Database);
        Assert.AreSame(_retryProvider, connection.RetryLogicProvider);
    }

    [TestMethod]
    public void GivenConfigureDelegate_WhenCreatingConnection_ThenApplyChangesToSelectedDatabase()
    {
        var builder = new ScopedSqlConnectionBuilder(_options, CreateContext(SecondKey), _retryProvider);

        using SqlConnection connection = builder.CreateConnection(b =>
        {
            b.InitialCatalog = "master";
            b.MaxPoolSize = 5;
        });

        Assert.AreEqual(SecondServer, connection.DataSource);
        Assert.AreEqual("master", connection.Database);
        Assert.AreEqual(5, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.AreEqual(SecondDatabase, builder.DefaultDatabase);
    }

    private static ISqlServerDatabaseContext CreateContext(string key)
    {
        ISqlServerDatabaseContext context = Substitute.For<ISqlServerDatabaseContext>();
        context.Key.Returns(key);
        return context;
    }
}
