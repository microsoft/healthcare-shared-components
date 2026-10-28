// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Configs;

[TestClass]
public class SqlServerDatabaseOptionsTests
{
    [TestMethod]
    public void GivenConnectionString_WhenValidating_ThenSucceed()
    {
        var options = new SqlServerDatabaseOptions { ConnectionString = "server=(local);Initial Catalog=Database" };

        Assert.IsTrue(DataAnnotationsValidator.TryValidate(options, out List<ValidationResult> results), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GivenMissingConnectionString_WhenValidating_ThenFail(string connectionString)
    {
        var options = new SqlServerDatabaseOptions { ConnectionString = connectionString };

        DataAnnotationsValidator.AssertSingleFailure(options, nameof(SqlServerDatabaseOptions.ConnectionString));
    }
}
