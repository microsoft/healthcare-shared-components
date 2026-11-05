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
public class SchemaObjectTests
{
    [TestMethod]
    public void GivenTableWithoutDatabase_WhenFormatted_ThenReturnSchemaQualifiedName()
    {
        var table = new Table("dbo.MyTable");

        Assert.AreEqual("dbo.MyTable", table.TableName);
        Assert.IsNull(table.DatabaseName);
        Assert.AreEqual("dbo.MyTable", table.ToString());
        Assert.AreEqual("dbo.MyTable", (string)table);
    }

    [TestMethod]
    [DataRow("MyDatabase", "[MyDatabase].dbo.MyTable")]
    [DataRow("my-database]", "[my-database]]].dbo.MyTable")]
    public void GivenTableWithDatabase_WhenFormatted_ThenReturnDatabaseQualifiedName(string databaseName, string expected)
    {
        var table = new Table("dbo.MyTable", databaseName);

        Assert.AreEqual("dbo.MyTable", table.TableName);
        Assert.AreEqual(databaseName, table.DatabaseName);
        Assert.AreEqual(expected, table.ToString());
        Assert.AreEqual(expected, (string)table);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void GivenInvalidDatabase_WhenCreatingTable_ThenThrowArgumentException(string databaseName)
    {
        Assert.Throws<ArgumentException>(() => new Table("dbo.MyTable", databaseName));
    }

    [TestMethod]
    public void GivenStoredProcedureWithoutDatabase_WhenFormatted_ThenReturnSchemaQualifiedName()
    {
        var procedure = new StoredProcedure("dbo.MyProcedure");

        Assert.AreEqual("dbo.MyProcedure", procedure.ProcedureName);
        Assert.IsNull(procedure.DatabaseName);
        Assert.AreEqual("dbo.MyProcedure", procedure.ToString());
        Assert.AreEqual("dbo.MyProcedure", (string)procedure);
    }

    [TestMethod]
    [DataRow("MyDatabase", "[MyDatabase].dbo.MyProcedure")]
    [DataRow("my-database]", "[my-database]]].dbo.MyProcedure")]
    public void GivenStoredProcedureWithDatabase_WhenFormatted_ThenReturnDatabaseQualifiedName(string databaseName, string expected)
    {
        var procedure = new StoredProcedure("dbo.MyProcedure", databaseName);

        Assert.AreEqual("dbo.MyProcedure", procedure.ProcedureName);
        Assert.AreEqual(databaseName, procedure.DatabaseName);
        Assert.AreEqual(expected, procedure.ToString());
        Assert.AreEqual(expected, (string)procedure);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void GivenInvalidDatabase_WhenCreatingStoredProcedure_ThenThrowArgumentException(string databaseName)
    {
        Assert.Throws<ArgumentException>(() => new StoredProcedure("dbo.MyProcedure", databaseName));
    }

    [TestMethod]
    public void GivenGeneratedTable_WhenCreatedWithDatabase_ThenReturnDatabaseQualifiedName()
    {
        Assert.AreEqual("dbo.InstanceSchema", SchemaShared.InstanceSchema.ToString());
        Assert.AreEqual("[MyDatabase].dbo.InstanceSchema", new SchemaShared.InstanceSchemaTable("MyDatabase").ToString());
    }

    [TestMethod]
    public void GivenGeneratedStoredProcedure_WhenPopulatingCommand_ThenUseQualifiedName()
    {
        using var sqlCommand = new SqlCommand();
        using var command = new SqlCommandWrapper(sqlCommand);

        SchemaShared.SelectCurrentSchemaVersion.PopulateCommand(command);
        Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
        Assert.AreEqual("dbo.SelectCurrentSchemaVersion", command.CommandText);

        new SchemaShared.SelectCurrentSchemaVersionProcedure("MyDatabase").PopulateCommand(command);
        Assert.AreEqual(CommandType.StoredProcedure, command.CommandType);
        Assert.AreEqual("[MyDatabase].dbo.SelectCurrentSchemaVersion", command.CommandText);
    }
}
