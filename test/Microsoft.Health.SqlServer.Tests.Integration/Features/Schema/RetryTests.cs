// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
public class RetryTests : SqlIntegrationTestBase
{
    public RetryTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    private const int RetryError = 8134; // divide by zero

    [Fact]
    public async Task RetriesBehavior()
    {
        try
        {
            await CreateTestTable();

            // For non query retries are handled by command retry
            await CreateTestStoredProcedureWithErrorBeforeSelect();
            Assert.False(await ErrorIsLogged());
            await ExecuteNonQuery();
            Assert.True(await ErrorIsLogged());

            // if error happens before select it is handled by command retry
            // same stored procedure as above just reader added
            await ResetError();
            Assert.False(await ErrorIsLogged());
            var results = await ExecuteQuery();
            Assert.True(await ErrorIsLogged());
            Assert.Equal(10, results.Count);

            await ResetError();
            Assert.False(await ErrorIsLogged());
            // if error happens in select it is not handled
            await CreateTestStoredProcedureWithErrorInSelect();
            try
            {
                results = await ExecuteQuery();
                Assert.Fail("This point should not be reached");
            }
            catch (SqlException ex)
            {
                Assert.Equal(RetryError, ex.Number);
            }
            Assert.True(await ErrorIsLogged());

            await ResetError();
            Assert.False(await ErrorIsLogged());
            // if error happens after select it is not handled
            await CreateTestStoredProcedureWithErrorAfterSelect();
            try
            {
                results = await ExecuteQuery();
                Assert.Fail("This point should not be reached");
            }
            catch (SqlException ex)
            {
                Assert.Equal(RetryError, ex.Number);
            }
            Assert.True(await ErrorIsLogged());
        }
        finally
        {
            await DropTestObjecs();
        }
    }

    private async Task ExecuteNonQuery()
    {
        using var conn = await ConnectionFactory.ObtainSqlConnectionWrapperAsync(CancellationToken.None, true);
        using var cmd = conn.CreateRetrySqlCommand();
        cmd.CommandText = "dbo.TestStoredProcedure";
        cmd.CommandType = CommandType.StoredProcedure;
        await cmd.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task<ReadOnlyList<long>> ExecuteQuery()
    {
        // this code is standard in FHIR, for example look at SearchImpl.

        List<long> results;

        using var conn = await ConnectionFactory.ObtainSqlConnectionWrapperAsync(CancellationToken.None, true);
        using var cmd = conn.CreateRetrySqlCommand();
        cmd.CommandText = "dbo.TestStoredProcedure";
        cmd.CommandType = CommandType.StoredProcedure;
        using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);
        results = new List<long>();
        while (await reader.ReadAsync(CancellationToken.None))
        {
            results.Add(reader.GetInt64(0));
        }

        await reader.NextResultAsync(CancellationToken.None);

        return results;
    }

    private async Task ResetError()
    {
        await ExecuteSql("TRUNCATE TABLE dbo.TestTable");
    }

    private async Task CreateTestStoredProcedureWithErrorBeforeSelect()
    {
        await ExecuteSql(@$"
CREATE OR ALTER PROCEDURE dbo.TestStoredProcedure
AS
set nocount on
DECLARE @RaiseError bit = 0
IF NOT EXISTS (SELECT * FROM dbo.TestTable)
BEGIN
  INSERT INTO dbo.TestTable (Id) SELECT 'TestError' 
  SET @RaiseError = 1 / 0
END
SELECT TOP 10 RowId = row_number() OVER (ORDER BY object_id) FROM sys.objects
             ");
    }

    private async Task CreateTestStoredProcedureWithErrorInSelect()
    {
        await ExecuteSql(@$"
CREATE OR ALTER PROCEDURE dbo.TestStoredProcedure
AS
set nocount on
DECLARE @RaiseError bit = 0
IF NOT EXISTS (SELECT * FROM dbo.TestTable)
BEGIN
  INSERT INTO dbo.TestTable (Id) SELECT 'TestError' 
  SET @RaiseError = 1
END
SELECT RowId / CASE WHEN RowId = 6 AND @RaiseError = 1 THEN 0 ELSE 1 END -- conditionally raise error on row 6
  FROM (SELECT TOP 10 RowId = row_number() OVER (ORDER BY object_id) FROM sys.objects) A
             ");
    }

    private async Task CreateTestStoredProcedureWithErrorAfterSelect()
    {
        await ExecuteSql(@$"
CREATE OR ALTER PROCEDURE dbo.TestStoredProcedure
AS
set nocount on
DECLARE @RaiseError bit = 0
IF NOT EXISTS (SELECT * FROM dbo.TestTable)
BEGIN
  INSERT INTO dbo.TestTable (Id) SELECT 'TestError' 
  SET @RaiseError = 1
END
SELECT TOP 10 RowId = row_number() OVER (ORDER BY object_id) FROM sys.objects
SET @RaiseError = 1 / 0
             ");
    }

    private async Task DropTestObjecs()
    {
        await ExecuteSql("IF object_id('dbo.TestTable') IS NOT NULL DROP TABLE dbo.TestTable");
        await ExecuteSql("IF object_id('dbo.TestStoredProcedure') IS NOT NULL DROP PROCEDURE dbo.TestStoredProcedure");
    }

    private async Task CreateTestTable()
    {
        await ExecuteSql("CREATE TABLE dbo.TestTable (Id varchar(100) PRIMARY KEY)");
    }

    private async Task ExecuteSql(string sql)
    {
        using var conn = await ConnectionFactory.ObtainSqlConnectionWrapperAsync(CancellationToken.None, true);
        using var cmd = conn.CreateRetrySqlCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task<bool> ErrorIsLogged()
    {
        using var conn = await ConnectionFactory.ObtainSqlConnectionWrapperAsync(CancellationToken.None, true);
        using var cmd = conn.CreateRetrySqlCommand();
        cmd.CommandText = "SELECT Id FROM dbo.TestTable";
        var id = await cmd.ExecuteScalarAsync(CancellationToken.None);
        return id != null;
    }
}
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

