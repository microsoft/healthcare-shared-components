// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Operations.UnitTests;

[TestClass]
public class OperationStatusExtensionsTests
{
    [TestMethod]
    [DataRow(OperationStatus.Unknown, false)]
    [DataRow(OperationStatus.NotStarted, true)]
    [DataRow(OperationStatus.Running, true)]
#pragma warning disable CS0618
    [DataRow(OperationStatus.Completed, false)]
#pragma warning restore CS0618
    [DataRow(OperationStatus.Succeeded, false)]
    [DataRow(OperationStatus.Failed, false)]
    [DataRow(OperationStatus.Canceled, false)]
    public void GivenStatus_WhenCheckingIfInProgress_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.AreEqual(expected, status.IsInProgress());

    [TestMethod]
    [DataRow(OperationStatus.Unknown, false)]
    [DataRow(OperationStatus.NotStarted, false)]
    [DataRow(OperationStatus.Running, false)]
#pragma warning disable CS0618
    [DataRow(OperationStatus.Completed, true)]
#pragma warning restore CS0618
    [DataRow(OperationStatus.Succeeded, true)]
    [DataRow(OperationStatus.Failed, true)]
    [DataRow(OperationStatus.Canceled, true)]
    public void GivenStatus_WhenCheckingIfStopped_ThenReturnProperValue(OperationStatus status, bool expected)
        => Assert.AreEqual(expected, status.IsStopped());
}
