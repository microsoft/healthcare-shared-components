// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema.Messages;

[TestClass]
public sealed class SchemaUpgradedNotificationTests
{
    [TestMethod]
    public void GivenNullDatabaseName_WhenCreatingNotification_ThenThrowsArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new SchemaUpgradedNotification(null, 1, false));

    [TestMethod]
    public void GivenEmptyDatabaseName_WhenCreatingNotification_ThenThrowsArgumentException()
        => Assert.ThrowsExactly<ArgumentException>(() => new SchemaUpgradedNotification(string.Empty, 1, false));

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void GivenInvalidVersion_WhenCreatingNotification_ThenThrowsArgumentOutOfRangeException(int version)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SchemaUpgradedNotification("MyDb", version, false));

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GivenValidArguments_WhenCreatingNotification_ThenPropertiesAreSet(bool isFullSchemaSnapshot)
    {
        var notification = new SchemaUpgradedNotification("MyDb", 3, isFullSchemaSnapshot);

        Assert.AreEqual("MyDb", notification.DatabaseName);
        Assert.AreEqual(3, notification.Version);
        Assert.AreEqual(isFullSchemaSnapshot, notification.IsFullSchemaSnapshot);
    }
}
