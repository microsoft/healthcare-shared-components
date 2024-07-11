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

    [Fact]
    public void GivenString_WhenParsingOperationIdExactly_ThenReturnGuid()
    {
        Guid expected = Guid.NewGuid();
        Assert.Equal(expected, OperationId.ParseExact(expected.ToString(OperationId.FormatSpecifier)));
    }

    [Fact]
    public void GivenNull_WhenParsingOperationIdExactly_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => OperationId.ParseExact(null!));

    [Fact]
    public void GivenInvalidString_WhenParsingOperationIdExactly_ThenThrowFormatException()
        => Assert.Throws<FormatException>(() => OperationId.ParseExact(Guid.NewGuid().ToString("X")));
}
