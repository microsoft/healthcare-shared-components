// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
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
    private readonly string _azureResource = "https://database.windows.net/.default";

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

    private void InitializeTest(SqlServerAuthenticationType sqlServerAuthenticationType)
    {
        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};",
            AuthenticationType = sqlServerAuthenticationType,
        };

        TokenCredential tokenCredential = Substitute.For<TokenCredential>();

        AccessToken token = new AccessToken(TestAccessToken, DateTime.Now.AddHours(5));

        tokenCredential.GetToken(new TokenRequestContext(new[] { _azureResource }), CancellationToken.None).Returns(token);

        var sqlConfigOptions = Options.Create(sqlServerDataStoreConfiguration);
        _sqlConnectionFactory = new ManagedIdentitySqlConnectionBuilder(
            new DefaultSqlConnectionStringProvider(sqlConfigOptions), SqlConfigurableRetryFactory.CreateNoneRetryProvider(), tokenCredential);
    }
}
