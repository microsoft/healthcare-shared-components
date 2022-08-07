// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.Operations.UnitTests;

public class OperationStatusExtensionsTests
{
    [Theory]
    [InlineData(OperationStatus.Unknown, false)]
    [InlineData(OperationStatus.NotStarted, true)]
    [InlineData(OperationStatus.Running, true)]
    [InlineData(OperationStatus.Succeeded, false)]
    [InlineData(OperationStatus.Failed, false)]
    [InlineData(OperationStatus.Canceled, false)]
    public void GivenStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.Equal(expected, status.IsInProgress());

    [Theory]
    [InlineData(OperationStatus.Unknown, false)]
    [InlineData(OperationStatus.NotStarted, false)]
    [InlineData(OperationStatus.Running, false)]
    [InlineData(OperationStatus.Succeeded, true)]
    [InlineData(OperationStatus.Failed, true)]
    [InlineData(OperationStatus.Canceled, true)]
    public void GivenStatus_WhenCheckingIfStopped_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.Equal(expected, status.IsStopped());
}
