// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.Tests.Integration;

[TestClass]
public sealed class ScopedSqlConnectionBuilderTests : SqlIntegrationTestBase
{
    private const string TestKey = "Test";
    private const string MasterKey = "Master";
    private const string MasterDatabase = "master";

    public ScopedSqlConnectionBuilderTests(TestContext testContext)
        : base(testContext)
    {
    }

    [TestMethod]
    [DataRow(TestKey, false)]
    [DataRow(MasterKey, true)]
    public async Task GivenDatabaseContext_WhenObtainingConnection_ThenConnectToSelectedDatabase(string key, bool isMaster)
    {
        string expectedDatabase = isMaster ? MasterDatabase : DatabaseName;
        string testConnectionString = Options.Databases.Values.Single().ConnectionString;
        var masterConnectionString = new SqlConnectionStringBuilder(testConnectionString) { InitialCatalog = MasterDatabase };

        IOptionsSnapshot<SqlServerDataStoreOptions> snapshot = Substitute.For<IOptionsSnapshot<SqlServerDataStoreOptions>>();
        snapshot.Value.Returns(new SqlServerDataStoreOptions
        {
            Databases = new Dictionary<string, SqlServerDatabaseOptions>
            {
                [TestKey] = new SqlServerDatabaseOptions { ConnectionString = testConnectionString },
                [MasterKey] = new SqlServerDatabaseOptions { ConnectionString = masterConnectionString.ToString() },
            },
        });

        ISqlServerDatabaseContext context = Substitute.For<ISqlServerDatabaseContext>();
        context.Key.Returns(key);

        SqlRetryLogicBaseProvider retryProvider = SqlConfigurableRetryFactory.CreateNoneRetryProvider();
        var builder = new ScopedSqlConnectionBuilder(snapshot, context, retryProvider);

        using var transactionHandler = new SqlTransactionHandler();
        var factory = new SqlConnectionWrapperFactory(transactionHandler, builder, retryProvider, Microsoft.Extensions.Options.Options.Create(snapshot.Value));

        Assert.AreEqual(expectedDatabase, factory.DefaultDatabase);

        using SqlConnectionWrapper wrapper = await factory.ObtainSqlConnectionWrapperAsync(CancellationToken.None);
        using SqlCommandWrapper command = wrapper.CreateNonRetrySqlCommand();
        command.CommandText = "SELECT DB_NAME()";

        Assert.AreEqual(expectedDatabase, (string)await command.ExecuteScalarAsync(CancellationToken.None));
    }
}
