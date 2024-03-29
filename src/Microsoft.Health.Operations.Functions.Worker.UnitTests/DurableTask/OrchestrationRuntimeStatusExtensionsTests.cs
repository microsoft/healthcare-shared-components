// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Xunit;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.DurableTask;

#pragma warning disable CS0618 // Allow the user of obsolete OrchestrationRuntimeStatus values methods

public class OrchestrationRuntimeStatusExtensionsTests
{
    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Running, true)]
    [InlineData(OrchestrationRuntimeStatus.Completed, false)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, true)]
    [InlineData(OrchestrationRuntimeStatus.Failed, false)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, false)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, false)]
    [InlineData(OrchestrationRuntimeStatus.Pending, true)]
    [InlineData(OrchestrationRuntimeStatus.Suspended, false)]
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.Equal(expected, runtimeStatus.IsInProgress());

    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Running, false)]
    [InlineData(OrchestrationRuntimeStatus.Completed, true)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, false)]
    [InlineData(OrchestrationRuntimeStatus.Failed, true)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, true)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, true)]
    [InlineData(OrchestrationRuntimeStatus.Pending, false)]
    [InlineData(OrchestrationRuntimeStatus.Suspended, true)]
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfStopped_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.Equal(expected, runtimeStatus.IsStopped());

    [Theory]
    [InlineData((OrchestrationRuntimeStatus)47, OperationStatus.Unknown)]
    [InlineData(OrchestrationRuntimeStatus.Running, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Completed, OperationStatus.Succeeded)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Failed, OperationStatus.Failed)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Pending, OperationStatus.NotStarted)]
    [InlineData(OrchestrationRuntimeStatus.Suspended, OperationStatus.Paused)]
    public void GivenOrchestrationRuntimeStatus_WhenConvertingToOperationStatus_ThenReturnCorrespondingValue(OrchestrationRuntimeStatus runtimeStatus, OperationStatus expected)
        => Assert.Equal(expected, runtimeStatus.ToOperationStatus());

    [Theory]
    [InlineData(OperationStatus.NotStarted, OrchestrationRuntimeStatus.Pending)]
    [InlineData(OperationStatus.Running, OrchestrationRuntimeStatus.Running)]
    [InlineData(OperationStatus.Completed, OrchestrationRuntimeStatus.Completed)]
    [InlineData(OperationStatus.Failed, OrchestrationRuntimeStatus.Failed)]
    [InlineData(OperationStatus.Canceled, OrchestrationRuntimeStatus.Terminated)]
    [InlineData(OperationStatus.Succeeded, OrchestrationRuntimeStatus.Completed)]
    [InlineData(OperationStatus.Paused, OrchestrationRuntimeStatus.Suspended)]
    public void GivenOperationStatus_WhenConvertingToOrchestrationRuntimeStatus_ThenReturnCorrespondingValue(OperationStatus status, OrchestrationRuntimeStatus expected)
        => Assert.Equal(expected, status.ToOrchestrationRuntimeStatus());

    [Theory]
    [InlineData((OperationStatus)47)]
    [InlineData(OperationStatus.Unknown)]
    public void GivenUnknownOperationStatus_WhenConvertingToOrchestrationRuntimeStatus_ThenThrowArgumentOutOfRangeException(OperationStatus status)
        => Assert.Throws<ArgumentOutOfRangeException>(() => status.ToOrchestrationRuntimeStatus());
}
