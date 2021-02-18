// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Configs;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features
{
    public class DefaultSqlConnectionStringProviderTests
    {
        [Fact]
        public void GivenNullSqlServerDataStoreConfiguration_CtorThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration: null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("server=(local);Initial Catalog=Dicom;Integrated Security=true")]
        public async Task GivenValidSqlServerDataStoreConfiguration_GetSqlConnectionString_ReturnsConnectionString(string sqlConnectionString)
        {
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration() { ConnectionString = sqlConnectionString };
            ISqlConnectionStringProvider sqlConnectionStringProvider = new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration);

            Assert.Equal(sqlConnectionString, await sqlConnectionStringProvider.GetSqlConnectionString(CancellationToken.None));
        }
    }
}
