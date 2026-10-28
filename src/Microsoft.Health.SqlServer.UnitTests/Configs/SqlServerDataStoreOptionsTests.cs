// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Configs;

[TestClass]
public class SqlServerDataStoreOptionsTests
{
    private const string DefaultKey = "Default";
    private const string FirstConnectionString = "server=(local);Initial Catalog=Database1";
    private const string SecondConnectionString = "server=(local);Initial Catalog=Database2";

    [TestMethod]
    public void GivenNewOptions_WhenReadingDefaults_ThenReturnExpectedValues()
    {
        var options = new SqlServerDataStoreOptions();

        Assert.AreEqual(TimeSpan.FromSeconds(30), options.CommandTimeout);
        Assert.AreEqual(TimeSpan.FromHours(4), options.StatementTimeout);
        Assert.IsNull(options.MaxPoolSize);
        Assert.IsNotNull(options.Retry);
        Assert.IsNotNull(options.SchemaOptions);
        Assert.IsFalse(options.SchemaOptions.AutomaticUpdatesEnabled);
        Assert.AreEqual(TimeSpan.FromMinutes(1), options.SchemaOptions.JobPollingFrequency);
        Assert.AreEqual(TimeSpan.FromMinutes(2), options.SchemaOptions.InstanceRecordExpirationTime);
        Assert.IsNull(options.SchemaOptions.CompletionSchemaVersion);
    }

    [TestMethod]
    public void GivenConfiguration_WhenBinding_ThenPopulateOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"{SqlServerDataStoreOptions.SectionName}:AllowDatabaseCreation"] = "true",
                [$"{SqlServerDataStoreOptions.SectionName}:CommandTimeout"] = "00:01:00",
                [$"{SqlServerDataStoreOptions.SectionName}:Databases:tenant1:ConnectionString"] = "server=(local);Initial Catalog=Database1",
                [$"{SqlServerDataStoreOptions.SectionName}:Databases:tenant2:ConnectionString"] = "server=(local);Initial Catalog=Database2",
                [$"{SqlServerDataStoreOptions.SectionName}:Initialize"] = "true",
                [$"{SqlServerDataStoreOptions.SectionName}:MaxPoolSize"] = "50",
                [$"{SqlServerDataStoreOptions.SectionName}:Region"] = "eastus2",
                [$"{SqlServerDataStoreOptions.SectionName}:SchemaOptions:AutomaticUpdatesEnabled"] = "true",
                [$"{SqlServerDataStoreOptions.SectionName}:SchemaOptions:CompletionSchemaVersion"] = "7",
                [$"{SqlServerDataStoreOptions.SectionName}:SchemaOptions:InstanceRecordExpirationTime"] = "00:05:00",
                [$"{SqlServerDataStoreOptions.SectionName}:SchemaOptions:JobPollingFrequency"] = "00:00:15",
                [$"{SqlServerDataStoreOptions.SectionName}:StatementTimeout"] = "02:00:00",
            })
            .Build();

        SqlServerDataStoreOptions options = configuration.GetSection(SqlServerDataStoreOptions.SectionName).Get<SqlServerDataStoreOptions>();

        Assert.IsTrue(options.AllowDatabaseCreation);
        Assert.AreEqual(TimeSpan.FromMinutes(1), options.CommandTimeout);
        Assert.HasCount(2, options.Databases);
        Assert.AreEqual("server=(local);Initial Catalog=Database1", options.Databases["tenant1"].ConnectionString);
        Assert.AreEqual("server=(local);Initial Catalog=Database2", options.Databases["tenant2"].ConnectionString);
        Assert.IsTrue(options.Initialize);
        Assert.AreEqual(50, options.MaxPoolSize);
        Assert.AreEqual("eastus2", options.Region);
        Assert.IsTrue(options.SchemaOptions.AutomaticUpdatesEnabled);
        Assert.AreEqual(7, options.SchemaOptions.CompletionSchemaVersion);
        Assert.AreEqual(TimeSpan.FromMinutes(5), options.SchemaOptions.InstanceRecordExpirationTime);
        Assert.AreEqual(TimeSpan.FromSeconds(15), options.SchemaOptions.JobPollingFrequency);
        Assert.AreEqual(TimeSpan.FromHours(2), options.StatementTimeout);
    }

    [TestMethod]
    public void GivenLegacyConnectionStringConfiguration_WhenBinding_ThenPopulateSingleDatabase()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"{SqlServerDataStoreOptions.SectionName}:ConnectionString"] = FirstConnectionString,
            })
            .Build();

        SqlServerDataStoreOptions options = configuration.GetSection(SqlServerDataStoreOptions.SectionName).Get<SqlServerDataStoreOptions>();

        KeyValuePair<string, SqlServerDatabaseOptions> database = Assert.ContainsSingle(options.Databases);
        Assert.AreEqual(DefaultKey, database.Key);
        Assert.AreEqual(FirstConnectionString, database.Value.ConnectionString);
        Assert.AreEqual(FirstConnectionString, options.ConnectionString);
    }

    [TestMethod]
    public void GivenNullDatabases_WhenGettingConnectionString_ThenReturnNull()
    {
        var options = new SqlServerDataStoreOptions { Databases = null };

        Assert.IsNull(options.ConnectionString);
    }

    [TestMethod]
    public void GivenNoDatabases_WhenGettingConnectionString_ThenReturnNull()
    {
        var options = new SqlServerDataStoreOptions { Databases = new Dictionary<string, SqlServerDatabaseOptions>() };

        Assert.IsNull(options.ConnectionString);
    }

    [TestMethod]
    public void GivenSingleDatabase_WhenGettingConnectionString_ThenReturnItsConnectionString()
    {
        var options = new SqlServerDataStoreOptions { Databases = CreateDatabases(("tenant1", FirstConnectionString)) };

        Assert.AreEqual(FirstConnectionString, options.ConnectionString);
    }

    [TestMethod]
    public void GivenMultipleDatabases_WhenGettingConnectionString_ThenReturnNull()
    {
        var options = new SqlServerDataStoreOptions { Databases = CreateDatabases(("tenant1", FirstConnectionString), ("tenant2", SecondConnectionString)) };

        Assert.IsNull(options.ConnectionString);
    }

    [TestMethod]
    public void GivenNoDatabases_WhenSettingConnectionString_ThenAddDefaultDatabase()
    {
        var options = new SqlServerDataStoreOptions { ConnectionString = FirstConnectionString };

        KeyValuePair<string, SqlServerDatabaseOptions> database = Assert.ContainsSingle(options.Databases);
        Assert.AreEqual(DefaultKey, database.Key);
        Assert.AreEqual(FirstConnectionString, database.Value.ConnectionString);
        Assert.AreEqual(FirstConnectionString, options.ConnectionString);
    }

    [TestMethod]
    public void GivenSingleDatabase_WhenSettingConnectionString_ThenReplaceWithDefaultDatabase()
    {
        var options = new SqlServerDataStoreOptions { Databases = CreateDatabases(("tenant1", FirstConnectionString)) };

        options.ConnectionString = SecondConnectionString;

        KeyValuePair<string, SqlServerDatabaseOptions> database = Assert.ContainsSingle(options.Databases);
        Assert.AreEqual(DefaultKey, database.Key);
        Assert.AreEqual(SecondConnectionString, database.Value.ConnectionString);
    }

    [TestMethod]
    public void GivenMultipleDatabases_WhenSettingConnectionString_ThenThrowInvalidOperationException()
    {
        IReadOnlyDictionary<string, SqlServerDatabaseOptions> databases = CreateDatabases(("tenant1", FirstConnectionString), ("tenant2", SecondConnectionString));
        var options = new SqlServerDataStoreOptions { Databases = databases };

        Assert.ThrowsExactly<InvalidOperationException>(() => options.ConnectionString = "server=(local);Initial Catalog=Other");
        Assert.AreSame(databases, options.Databases);
    }

    [TestMethod]
    public void GivenValidOptions_WhenValidating_ThenSucceed()
    {
        SqlServerDataStoreOptions options = CreateValidOptions();

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    public void GivenMultipleDatabases_WhenValidating_ThenSucceed()
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Databases = CreateDatabases(("tenant1", FirstConnectionString), ("tenant2", SecondConnectionString));

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    public void GivenNullDatabases_WhenValidating_ThenFail()
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Databases = null;

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.Databases));
    }

    [TestMethod]
    public void GivenNoDatabases_WhenValidating_ThenFail()
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Databases = new Dictionary<string, SqlServerDatabaseOptions>();

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.Databases));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GivenDatabaseWithMissingConnectionString_WhenValidating_ThenFail(string connectionString)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Databases = CreateDatabases(("tenant1", FirstConnectionString), ("tenant2", connectionString));

        ValidationResult result = DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.Databases));
        Assert.Contains("[tenant2]", result.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("[tenant1]", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenNullDatabaseEntry_WhenValidating_ThenFail()
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Databases = new Dictionary<string, SqlServerDatabaseOptions> { ["tenant1"] = null };

        ValidationResult result = DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.Databases));
        Assert.Contains("[tenant1]", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GivenMissingRegion_WhenValidating_ThenFail(string region)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.Region = region;

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.Region));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(1)]
    [DataRow(SqlConstants.MaxPoolSizeLimit)]
    public void GivenMaxPoolSizeInRange_WhenValidating_ThenSucceed(int? maxPoolSize)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.MaxPoolSize = maxPoolSize;

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(SqlConstants.MaxPoolSizeLimit + 1)]
    public void GivenMaxPoolSizeOutOfRange_WhenValidating_ThenFail(int maxPoolSize)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.MaxPoolSize = maxPoolSize;

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.MaxPoolSize));
    }

    [TestMethod]
    [DataRow("00:00:01")]
    [DataRow("00:30:00")]
    [DataRow("1.00:00:00")]
    public void GivenTimeoutsInRange_WhenValidating_ThenSucceed(string timeout)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.CommandTimeout = TimeSpan.Parse(timeout, CultureInfo.InvariantCulture);
        options.StatementTimeout = TimeSpan.Parse(timeout, CultureInfo.InvariantCulture);

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow("-00:00:01")]
    [DataRow("00:00:00")]
    [DataRow("00:00:00.999")]
    [DataRow("1.00:00:01")]
    public void GivenCommandTimeoutOutOfRange_WhenValidating_ThenFail(string timeout)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.CommandTimeout = TimeSpan.Parse(timeout, CultureInfo.InvariantCulture);

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.CommandTimeout));
    }

    [TestMethod]
    [DataRow("-00:00:01")]
    [DataRow("00:00:00")]
    [DataRow("00:00:00.999")]
    [DataRow("1.00:00:01")]
    public void GivenStatementTimeoutOutOfRange_WhenValidating_ThenFail(string timeout)
    {
        SqlServerDataStoreOptions options = CreateValidOptions();
        options.StatementTimeout = TimeSpan.Parse(timeout, CultureInfo.InvariantCulture);

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDataStoreOptions.StatementTimeout));
    }

    private static SqlServerDataStoreOptions CreateValidOptions()
        => new()
        {
            Databases = CreateDatabases((DefaultKey, FirstConnectionString)),
            Region = "eastus2",
        };

    private static Dictionary<string, SqlServerDatabaseOptions> CreateDatabases(params (string Key, string ConnectionString)[] databases)
        => databases.ToDictionary(x => x.Key, x => new SqlServerDatabaseOptions { ConnectionString = x.ConnectionString });
}
