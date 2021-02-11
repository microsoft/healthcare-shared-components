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

        [Fact]
        public void GivenNullConnectionString_CtorThrows()
        {
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration() { ConnectionString = null };
            Assert.Throws<ArgumentNullException>(() => new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration));
        }

        [Fact]
        public void GivenEmptyConnectionString_CtorThrows()
        {
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration() { ConnectionString = string.Empty };
            Assert.Throws<ArgumentException>(() => new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration));
        }

        [Fact]
        public async Task GivenValidSqlServerDataStoreConfiguration_GetSqlConnectionString_ReturnsConnectionString()
        {
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration() { ConnectionString = $"server=(local);Initial Catalog=Dicom;Integrated Security=true" };
            ISqlConnectionStringProvider sqlConnectionStringProvider = new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration);

            Assert.Equal(sqlServerDataStoreConfiguration.ConnectionString, await sqlConnectionStringProvider.GetSqlConnectionString(CancellationToken.None));
        }
    }
}
