// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Health.Operations.UnitTests;

public class OperationIdTests
{
    [Fact]
    public void GivenOperationIdClass_WhenGeneratingNewId_ThenReturnProperlyFormattedString()
    {
        string actual = OperationId.Generate();
        Assert.True(Guid.TryParseExact(actual, OperationId.FormatSpecifier, out Guid _));
    }
}
