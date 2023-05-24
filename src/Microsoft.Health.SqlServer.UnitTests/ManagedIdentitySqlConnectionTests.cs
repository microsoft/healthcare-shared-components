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

    private ManagedIdentitySqlConnectionBuilder _sqlConnectionFactory;



    [Theory]
    [InlineData(SqlServerAuthenticationType.ManagedIdentity)]
    [InlineData(SqlServerAuthenticationType.WorkloadIdentity)]
    public async Task GivenManagedIdentityConnectionType_WhenSqlConnectionRequested_AccessTokenIsSet(SqlServerAuthenticationType sqlServerAuthenticationType)
    {
        InitializeTest(sqlServerAuthenticationType);

        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync().ConfigureAwait(false);

        Assert.Equal(TestAccessToken, sqlConnection.AccessToken);
    }

    [Theory]
    [InlineData(SqlServerAuthenticationType.ManagedIdentity)]
    [InlineData(SqlServerAuthenticationType.WorkloadIdentity)]
    public async Task GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet(SqlServerAuthenticationType sqlServerAuthenticationType)
    {
        InitializeTest(sqlServerAuthenticationType);
        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync().ConfigureAwait(false);

        Assert.Equal(DatabaseName, sqlConnection.Database);
    }

    [Theory]
    [InlineData(SqlServerAuthenticationType.ManagedIdentity)]
    [InlineData(SqlServerAuthenticationType.WorkloadIdentity)]
    public async Task GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet(SqlServerAuthenticationType sqlServerAuthenticationType)
    {
        InitializeTest(sqlServerAuthenticationType);
        SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync(MasterDatabase).ConfigureAwait(false);

        Assert.Equal(MasterDatabase, sqlConnection.Database);
    }

    private void InitializeTest(SqlServerAuthenticationType sqlServerAutehnticationType)
    {
        var azureTokenCredentialProvider = Substitute.For<IAzureTokenCredentialProvider>();
        azureTokenCredentialProvider.GetAccessTokenAsync().Returns(Task.FromResult(TestAccessToken));

        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};",
            AuthenticationType = sqlServerAutehnticationType,
        };

        var sqlConfigOptions = Options.Create(sqlServerDataStoreConfiguration);
        _sqlConnectionFactory = new ManagedIdentitySqlConnectionBuilder(azureTokenCredentialProvider,
            new DefaultSqlConnectionStringProvider(sqlConfigOptions), SqlConfigurableRetryFactory.CreateNoneRetryProvider());
    }
}
