// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SchemaManager.UnitTests;

[TestClass]
public class BaseSchemaRunnerTests
{
    [TestMethod]
    public async Task GivenWriteGateReturnsFalse_WhenEnsuringBaseSchema_ThenDoesNotInitializeDatabase()
    {
        ISchemaWriteGate writeGate = Substitute.For<ISchemaWriteGate>();
        writeGate.CanWriteAsync(Arg.Any<CancellationToken>()).Returns(false);
        ISchemaManagerDataStore dataStore = Substitute.For<ISchemaManagerDataStore>();
        SqlConnectionWrapperFactory connectionFactory = Substitute.For<SqlConnectionWrapperFactory>(
            Substitute.For<SqlTransactionHandler>(),
            Substitute.For<ISqlConnectionBuilder>(),
            Substitute.For<SqlRetryLogicBaseProvider>(),
            Options.Create(new SqlServerDataStoreConfiguration()));
        var runner = new BaseSchemaRunner(connectionFactory, dataStore, writeGate, NullLogger<BaseSchemaRunner>.Instance);

        using var cancellationTokenSource = new CancellationTokenSource();
        await runner.EnsureBaseSchemaExistsAsync(cancellationTokenSource.Token);

        await writeGate.Received(1).CanWriteAsync(cancellationTokenSource.Token);
        await connectionFactory.DidNotReceiveWithAnyArgs().ObtainSqlConnectionWrapperAsync(default);
        await dataStore.DidNotReceiveWithAnyArgs().BaseSchemaExistsAsync(default);
        await dataStore.DidNotReceiveWithAnyArgs().ExecuteScriptAsync(default, default);
    }
}
