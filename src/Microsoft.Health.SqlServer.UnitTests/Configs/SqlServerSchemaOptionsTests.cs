// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Configs;

[TestClass]
public class SqlServerSchemaOptionsTests
{
    [TestMethod]
    public void GivenDefaultOptions_WhenValidating_ThenSucceed()
    {
        Assert.IsTrue(DataAnnotationsValidator.TryValidate(new SqlServerSchemaOptions(), out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow("00:00:01")]
    [DataRow("00:05:00")]
    [DataRow("1.00:00:00")]
    public void GivenJobPollingFrequencyInRange_WhenValidating_ThenSucceed(string frequency)
    {
        var options = new SqlServerSchemaOptions { JobPollingFrequency = TimeSpan.Parse(frequency, CultureInfo.InvariantCulture) };

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow("-00:00:01")]
    [DataRow("00:00:00")]
    [DataRow("1.00:00:01")]
    public void GivenJobPollingFrequencyOutOfRange_WhenValidating_ThenFail(string frequency)
    {
        var options = new SqlServerSchemaOptions { JobPollingFrequency = TimeSpan.Parse(frequency, CultureInfo.InvariantCulture) };

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerSchemaOptions.JobPollingFrequency));
    }

    [TestMethod]
    [DataRow("00:01:00")]
    [DataRow("01:00:00")]
    [DataRow("1.00:00:00")]
    public void GivenInstanceRecordExpirationTimeInRange_WhenValidating_ThenSucceed(string expiration)
    {
        var options = new SqlServerSchemaOptions { InstanceRecordExpirationTime = TimeSpan.Parse(expiration, CultureInfo.InvariantCulture) };

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow("00:00:00")]
    [DataRow("00:00:59")]
    [DataRow("1.00:00:01")]
    public void GivenInstanceRecordExpirationTimeOutOfRange_WhenValidating_ThenFail(string expiration)
    {
        var options = new SqlServerSchemaOptions { InstanceRecordExpirationTime = TimeSpan.Parse(expiration, CultureInfo.InvariantCulture) };

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerSchemaOptions.InstanceRecordExpirationTime));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(1)]
    [DataRow(int.MaxValue)]
    public void GivenCompletionSchemaVersionInRange_WhenValidating_ThenSucceed(int? version)
    {
        var options = new SqlServerSchemaOptions { CompletionSchemaVersion = version };

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void GivenCompletionSchemaVersionOutOfRange_WhenValidating_ThenFail(int version)
    {
        var options = new SqlServerSchemaOptions { CompletionSchemaVersion = version };

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerSchemaOptions.CompletionSchemaVersion));
    }
}
