// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

[TestClass]
public class StoredProcedureTests
{
    [TestMethod]
    public void GivenGeneratedStoredProcedure_WhenPopulatingCommandWithoutDatabase_ThenUseSchemaQualifiedName()
    {
        using var sqlCommand = new SqlCommand();
        using var command = new SqlCommandWrapper(sqlCommand);

        SchemaShared.GetInstanceSchemaByName.PopulateCommand(command, "foo");

        Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
        Assert.AreEqual("dbo.GetInstanceSchemaByName", command.CommandText);
        Assert.AreEqual("foo", command.Parameters["@name"].Value);
    }

    [TestMethod]
    [DataRow("MyDatabase", "[MyDatabase].dbo.GetInstanceSchemaByName")]
    [DataRow("my-database]", "[my-database]]].dbo.GetInstanceSchemaByName")]
    public void GivenGeneratedStoredProcedure_WhenPopulatingCommandWithDatabase_ThenUseDatabaseQualifiedName(string databaseName, string expected)
    {
        using var sqlCommand = new SqlCommand();
        using var command = new SqlCommandWrapper(sqlCommand);

        SchemaShared.GetInstanceSchemaByName.PopulateCommand(command, databaseName, "foo");

        Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
        Assert.AreEqual(expected, command.CommandText);
        Assert.AreEqual("foo", command.Parameters["@name"].Value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void GivenGeneratedStoredProcedure_WhenPopulatingCommandWithInvalidDatabase_ThenThrowArgumentException(string databaseName)
    {
        using var sqlCommand = new SqlCommand();
        using var command = new SqlCommandWrapper(sqlCommand);

        Assert.Throws<ArgumentException>(() => SchemaShared.SelectCurrentSchemaVersion.PopulateCommand(command, databaseName));
    }
}
