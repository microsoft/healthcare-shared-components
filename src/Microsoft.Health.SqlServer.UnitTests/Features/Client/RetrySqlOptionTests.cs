// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client
{
    public class RetrySqlOptionTests
    {
        [Fact]
        public void GivenASqlCommandWrapper_ItsRetryPolicy_IsSet()
        {
            var retryOption = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 3,
            });
            var sqlCommandWrapper = new RetrySqlCommandWrapper(
                                        new SqlCommandWrapper(new Data.SqlClient.SqlCommand()),
                                        retryOption);

            Assert.True(sqlCommandWrapper.RetryLogicProvider == retryOption);
        }

        [Fact]
        public async Task GivenASqlConnectionWrapper_ItsRetryPolicy_IsSet()
        {
            var retryOption = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 3,
            });
            var options = Substitute.For<IOptions<SqlServerDataStoreConfiguration>>();
            options.Value.Returns(new SqlServerDataStoreConfiguration() { ConnectionString = "server=(local);Initial Catalog=DatabaseName;Integrated Security=true" });
            var sqlConnectionBuilder = new DefaultSqlConnectionBuilder(new DefaultSqlConnectionStringProvider(options), retryOption);
            var sqlConnection = await sqlConnectionBuilder.GetSqlConnectionAsync(null);
            Assert.True(sqlConnection.RetryLogicProvider == retryOption);
        }
    }
}
