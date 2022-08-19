// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
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
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.Equal(expected, runtimeStatus.IsInProgress());

    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Unknown, false)]
    [InlineData(OrchestrationRuntimeStatus.Running, false)]
    [InlineData(OrchestrationRuntimeStatus.Completed, true)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, false)]
    [InlineData(OrchestrationRuntimeStatus.Failed, true)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, true)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, true)]
    [InlineData(OrchestrationRuntimeStatus.Pending, false)]
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfStopped_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.Equal(expected, runtimeStatus.IsStopped());

    [Theory]
    [InlineData((OrchestrationRuntimeStatus)47, OperationStatus.Unknown)]
    [InlineData(OrchestrationRuntimeStatus.Unknown, OperationStatus.Unknown)]
    [InlineData(OrchestrationRuntimeStatus.Running, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Completed, OperationStatus.Succeeded)]
    [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, OperationStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Failed, OperationStatus.Failed)]
    [InlineData(OrchestrationRuntimeStatus.Canceled, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, OperationStatus.Canceled)]
    [InlineData(OrchestrationRuntimeStatus.Pending, OperationStatus.NotStarted)]
    public void GivenOrchestrationRuntimeStatus_WhenConvertingToOperationStatus_ThenReturnCorrespondingValue(OrchestrationRuntimeStatus runtimeStatus, OperationStatus expected)
        => Assert.Equal(expected, runtimeStatus.ToOperationStatus());

    [Theory]
    [InlineData((OperationStatus)47, OrchestrationRuntimeStatus.Unknown)]
    [InlineData(OperationStatus.Unknown, OrchestrationRuntimeStatus.Unknown)]
    [InlineData(OperationStatus.Running, OrchestrationRuntimeStatus.Running)]
#pragma warning disable CS0618
    [InlineData(OperationStatus.Completed, OrchestrationRuntimeStatus.Completed)]
#pragma warning restore CS0618
    [InlineData(OperationStatus.Succeeded, OrchestrationRuntimeStatus.Completed)]
    [InlineData(OperationStatus.Failed, OrchestrationRuntimeStatus.Failed)]
    [InlineData(OperationStatus.Canceled, OrchestrationRuntimeStatus.Canceled)]
    [InlineData(OperationStatus.NotStarted, OrchestrationRuntimeStatus.Pending)]
    public void GivenOperationStatus_WhenConvertingToOrchestrationRuntimeStatus_ThenReturnCorrespondingValue(OperationStatus status, OrchestrationRuntimeStatus expected)
        => Assert.Equal(expected, status.ToOrchestrationRuntimeStatus());

    [Theory]
    [InlineData((OperationStatus)47, OrchestrationRuntimeStatus.Unknown)]
    [InlineData(OperationStatus.Unknown, OrchestrationRuntimeStatus.Unknown)]
    [InlineData(OperationStatus.Running, OrchestrationRuntimeStatus.Running, OrchestrationRuntimeStatus.ContinuedAsNew)]
#pragma warning disable CS0618
    [InlineData(OperationStatus.Completed, OrchestrationRuntimeStatus.Completed)]
#pragma warning restore CS0618
    [InlineData(OperationStatus.Succeeded, OrchestrationRuntimeStatus.Completed)]
    [InlineData(OperationStatus.Failed, OrchestrationRuntimeStatus.Failed)]
    [InlineData(OperationStatus.Canceled, OrchestrationRuntimeStatus.Canceled, OrchestrationRuntimeStatus.Terminated)]
    [InlineData(OperationStatus.NotStarted, OrchestrationRuntimeStatus.Pending)]
    public void GivenOperationStatus_WhenConvertingToOrchestrationRuntimeStatuses_ThenReturnCorrespondingValue(OperationStatus status, params OrchestrationRuntimeStatus[] expected)
        => Assert.True(expected.SequenceEqual(status.ToOrchestrationRuntimeStatuses()));
}
