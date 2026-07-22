// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

public class VectorSchemaModelTests
{
    [Fact]
    public void GivenVectorColumn_WhenModelIsGenerated_ThenColumnNameIsAvailable()
    {
        Assert.Equal("Embedding", VectorSchema.VectorTestTable.Embedding);
    }
}
