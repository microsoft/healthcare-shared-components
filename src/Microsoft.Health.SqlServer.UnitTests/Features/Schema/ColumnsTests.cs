// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlTypes;
using System.IO;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

public class ColumnsTests
{
    [Fact]
    public void GivenSqlDataRecordWithVarBinaryColumn_WhenSetVarBinaryValueTwice_ThenFirstValueShouldBeCleaned()
    {
        VarBinaryColumn varBinaryColumn = new VarBinaryColumn("Col1", -1);
        SqlDataRecord record = new SqlDataRecord(varBinaryColumn.Metadata);

        byte[] data1 = new byte[] { 1, 1, 1, 1 };
        byte[] data2 = new byte[] { 1, 1 };
        using Stream input1 = new MemoryStream(data1);
        using Stream input2 = new MemoryStream(data2);

        varBinaryColumn.Set(record, 0, input1);
        Assert.Equal(data1, ((SqlBinary)record.GetSqlValue(0)).Value);
        varBinaryColumn.Set(record, 0, input2);
        Assert.Equal(data2, ((SqlBinary)record.GetSqlValue(0)).Value);
    }
}
