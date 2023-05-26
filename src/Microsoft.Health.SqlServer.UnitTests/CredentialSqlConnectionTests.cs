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

public class CredentialSqlConnectionTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string TestAccessToken = "test token";

    private readonly CredentialSqlConnectionBuilder _sqlConnectionFactory;

    public CredentialSqlConnectionTests()
    {
        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};",
        };

        TokenCredential tokenCredential = Substitute.For<TokenCredential>();

        AccessToken token = new AccessToken(TestAccessToken, DateTime.Now.AddHours(5));

        tokenCredential.GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>()).Returns(token);

        var sqlConfigOptions = Options.Create(sqlServerDataStoreConfiguration);
        _sqlConnectionFactory = new CredentialSqlConnectionBuilder(
            new DefaultSqlConnectionStringProvider(sqlConfigOptions), SqlConfigurableRetryFactory.CreateNoneRetryProvider(), tokenCredential);
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
