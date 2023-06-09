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

namespace Microsoft.Health.SqlServer.UnitTests;

public class ManagedIdentitySqlConnectionTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string TestAccessToken = "test token";
    private const string AzureResource = "https://database.windows.net/";

    private readonly ManagedIdentitySqlConnectionBuilder _sqlConnectionFactory;

    public ManagedIdentitySqlConnectionTests()
    {
        var accessTokenHandler = Substitute.For<IAccessTokenHandler>();
        accessTokenHandler.GetAccessTokenAsync(AzureResource).Returns(Task.FromResult(TestAccessToken));

        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};",
            AuthenticationType = SqlServerAuthenticationType.ManagedIdentity,
        };

        var sqlConfigOptions = Options.Create(sqlServerDataStoreConfiguration);
        _sqlConnectionFactory = new ManagedIdentitySqlConnectionBuilder(
            new DefaultSqlConnectionStringProvider(sqlConfigOptions), accessTokenHandler, SqlConfigurableRetryFactory.CreateNoneRetryProvider());
    }

    [Fact]
    public async Task GivenManagedIdentityConnectionType_WhenSqlConnectionRequested_AccessTokenIsSet()
    {
        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync().ConfigureAwait(false);

        Assert.Equal(TestAccessToken, sqlConnection.AccessToken);
    }

    [Fact]
    public async Task GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet()
    {
        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync().ConfigureAwait(false);

        Assert.Equal(DatabaseName, sqlConnection.Database);
    }

    [Fact]
    public async Task GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet()
    {
        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync(MasterDatabase).ConfigureAwait(false);

        Assert.Equal(MasterDatabase, sqlConnection.Database);
    }
}
