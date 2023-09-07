// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features;

public class DefaultSqlConnectionTests
{
    private const string DatabaseName = "Dicom";
    private const string ServerName = "(local)";
    private const string MasterDatabase = "master";
    private const string DefaultConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Encrypt=true";

    private readonly SqlRetryLogicBaseProvider _retryProvider = Substitute.For<SqlRetryLogicBaseProvider>();

    [Fact]
    public void GivenDefaultSettings_WhenSqlConnectionRequested_ThenReturnSameValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection();
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(DatabaseName)]
    [InlineData(MasterDatabase)]
    [InlineData("fhir")]
    public void GivenInitialCatalogOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(string initialCatalog)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(initialCatalog);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(initialCatalog, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GivenMaxPoolOverride_WhenSqlConnectionRequested_ThenReturnModifiedValue(int maxPoolSize)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration { ConnectionString = DefaultConnectionString });
        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection(maxPoolSize: maxPoolSize);
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Equal(maxPoolSize, new SqlConnectionStringBuilder(connection.ConnectionString).MaxPoolSize);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);
    }

    [Fact]
    [Obsolete("Test should be removed when AuthenticationType is removed.")]
    public void GivenManagedIdentity_WhenSqlConnectionRequested_ThenReturnModifiedValue()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration
        {
            AuthenticationType = SqlServerAuthenticationType.ManagedIdentity,
            ConnectionString = DefaultConnectionString,
            ManagedIdentityClientId = Guid.NewGuid().ToString(),
        });

        var connectionBuilder = new DefaultSqlConnectionBuilder(options, _retryProvider);

        Assert.Equal(DatabaseName, connectionBuilder.DefaultDatabase);

        using SqlConnection connection = connectionBuilder.GetSqlConnection();
        Assert.Equal(ServerName, connection.DataSource);
        Assert.Equal(DatabaseName, connection.Database);
        Assert.Same(_retryProvider, connection.RetryLogicProvider);

        var actual = new SqlConnectionStringBuilder(connection.ConnectionString);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, actual.Authentication);
        Assert.Equal(options.Value.ManagedIdentityClientId, actual.UserID);
    }
}
