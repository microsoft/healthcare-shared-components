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
#pragma warning disable CS0618
    [InlineData(OperationStatus.Completed, false)]
#pragma warning restore CS0618
    [InlineData(OperationStatus.Succeeded, false)]
    [InlineData(OperationStatus.Failed, false)]
    [InlineData(OperationStatus.Canceled, false)]
    public void GivenStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.Equal(expected, status.IsInProgress());

    [Theory]
    [InlineData(OperationStatus.Unknown, false)]
    [InlineData(OperationStatus.NotStarted, false)]
    [InlineData(OperationStatus.Running, false)]
#pragma warning disable CS0618
    [InlineData(OperationStatus.Completed, true)]
#pragma warning restore CS0618
    [InlineData(OperationStatus.Succeeded, true)]
    [InlineData(OperationStatus.Failed, true)]
    [InlineData(OperationStatus.Canceled, true)]
    public void GivenStatus_WhenCheckingIfStopped_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.Equal(expected, status.IsStopped());
}
