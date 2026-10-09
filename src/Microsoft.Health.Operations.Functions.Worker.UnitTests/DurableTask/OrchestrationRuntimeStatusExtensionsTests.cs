// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.DurableTask;

#pragma warning disable CS0618 // Allow the user of obsolete OrchestrationRuntimeStatus values methods

[TestClass]
public class OrchestrationRuntimeStatusExtensionsTests
{
    [TestMethod]
    [DataRow(OrchestrationRuntimeStatus.Running, true)]
    [DataRow(OrchestrationRuntimeStatus.Completed, false)]
    [DataRow(OrchestrationRuntimeStatus.ContinuedAsNew, true)]
    [DataRow(OrchestrationRuntimeStatus.Failed, false)]
    [DataRow(OrchestrationRuntimeStatus.Canceled, false)]
    [DataRow(OrchestrationRuntimeStatus.Terminated, false)]
    [DataRow(OrchestrationRuntimeStatus.Pending, true)]
    [DataRow(OrchestrationRuntimeStatus.Suspended, false)]
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.AreEqual(expected, runtimeStatus.IsInProgress());

    [TestMethod]
    [DataRow(OrchestrationRuntimeStatus.Running, false)]
    [DataRow(OrchestrationRuntimeStatus.Completed, true)]
    [DataRow(OrchestrationRuntimeStatus.ContinuedAsNew, false)]
    [DataRow(OrchestrationRuntimeStatus.Failed, true)]
    [DataRow(OrchestrationRuntimeStatus.Canceled, true)]
    [DataRow(OrchestrationRuntimeStatus.Terminated, true)]
    [DataRow(OrchestrationRuntimeStatus.Pending, false)]
    [DataRow(OrchestrationRuntimeStatus.Suspended, true)]
    public void GivenOrchestrationRuntimeStatus_WhenCheckingIfStopped_ThenReturnProperValue(OrchestrationRuntimeStatus runtimeStatus, bool expected)
        => Assert.AreEqual(expected, runtimeStatus.IsStopped());

    [TestMethod]
    [DataRow((OrchestrationRuntimeStatus)47, OperationStatus.Unknown)]
    [DataRow(OrchestrationRuntimeStatus.Running, OperationStatus.Running)]
    [DataRow(OrchestrationRuntimeStatus.Completed, OperationStatus.Succeeded)]
    [DataRow(OrchestrationRuntimeStatus.ContinuedAsNew, OperationStatus.Running)]
    [DataRow(OrchestrationRuntimeStatus.Failed, OperationStatus.Failed)]
    [DataRow(OrchestrationRuntimeStatus.Canceled, OperationStatus.Canceled)]
    [DataRow(OrchestrationRuntimeStatus.Terminated, OperationStatus.Canceled)]
    [DataRow(OrchestrationRuntimeStatus.Pending, OperationStatus.NotStarted)]
    [DataRow(OrchestrationRuntimeStatus.Suspended, OperationStatus.Paused)]
    public void GivenOrchestrationRuntimeStatus_WhenConvertingToOperationStatus_ThenReturnCorrespondingValue(OrchestrationRuntimeStatus runtimeStatus, OperationStatus expected)
        => Assert.AreEqual(expected, runtimeStatus.ToOperationStatus());

    [TestMethod]
    [DataRow(OperationStatus.NotStarted, OrchestrationRuntimeStatus.Pending)]
    [DataRow(OperationStatus.Running, OrchestrationRuntimeStatus.Running)]
    [DataRow(OperationStatus.Completed, OrchestrationRuntimeStatus.Completed)]
    [DataRow(OperationStatus.Failed, OrchestrationRuntimeStatus.Failed)]
    [DataRow(OperationStatus.Canceled, OrchestrationRuntimeStatus.Terminated)]
    [DataRow(OperationStatus.Succeeded, OrchestrationRuntimeStatus.Completed)]
    [DataRow(OperationStatus.Paused, OrchestrationRuntimeStatus.Suspended)]
    public void GivenOperationStatus_WhenConvertingToOrchestrationRuntimeStatus_ThenReturnCorrespondingValue(OperationStatus status, OrchestrationRuntimeStatus expected)
        => Assert.AreEqual(expected, status.ToOrchestrationRuntimeStatus());

    [TestMethod]
    [DataRow((OperationStatus)47)]
    [DataRow(OperationStatus.Unknown)]
    public void GivenUnknownOperationStatus_WhenConvertingToOrchestrationRuntimeStatus_ThenThrowArgumentOutOfRangeException(OperationStatus status)
        => Assert.Throws<ArgumentOutOfRangeException>(() => status.ToOrchestrationRuntimeStatus());
}
