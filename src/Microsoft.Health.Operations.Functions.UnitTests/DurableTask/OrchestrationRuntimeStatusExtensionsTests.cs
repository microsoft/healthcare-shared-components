﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Operations.Functions.DurableTask;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.DurableTask;

public class OrchestrationRuntimeStatusExtensionsTests
{
    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Unknown, false)]
    [InlineData(OrchestrationRuntimeStatus.Running, true)]
    [InlineData(OrchestrationRuntimeStatus.Completed, false)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, true)]
    [InlineData(OrchestrationRuntimeStatus.Failed, false)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, false)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, false)]
    [InlineData(OrchestrationRuntimeStatus.Pending, true)]
    public void GivenStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OrchestrationRuntimeStatus status, bool expected)
        => Assert.Equal(expected, status.IsInProgress());

    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Unknown, false)]
    [InlineData(OrchestrationRuntimeStatus.Running, false)]
    [InlineData(OrchestrationRuntimeStatus.Completed, true)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, false)]
    [InlineData(OrchestrationRuntimeStatus.Failed, true)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, true)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, true)]
    [InlineData(OrchestrationRuntimeStatus.Pending, false)]
    public void GivenStatus_WhenCheckingIfStopped_ThenReturnProperValue(OrchestrationRuntimeStatus status, bool expected)
        => Assert.Equal(expected, status.IsStopped());

    [Theory]
    [InlineData((OrchestrationRuntimeStatus)47, OperationStatus.Unknown)]
    [InlineData(OrchestrationRuntimeStatus.Unknown, OperationStatus.Unknown)]
    [InlineData(OrchestrationRuntimeStatus.Running, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Completed, OperationStatus.Completed)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Failed, OperationStatus.Failed)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Pending, OperationStatus.NotStarted)]
    public void GivenStatus_WhenConvertingToOperationStatus_ThenReturnCorrespondingValue(OrchestrationRuntimeStatus status, OperationStatus expected)
        => Assert.Equal(expected, status.ToOperationStatus());
}
