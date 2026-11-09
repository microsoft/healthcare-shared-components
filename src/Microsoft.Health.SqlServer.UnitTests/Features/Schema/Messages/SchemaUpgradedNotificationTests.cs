// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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

    [TestMethod]
    public async Task GivenNullMediator_WhenNotifyingSchemaUpgraded_ThenThrowsArgumentNullException()
        => await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => SchemaMediatorExtensions.NotifySchemaUpgradedAsync(null, "MyDb", 1, false));

    [TestMethod]
    public async Task GivenMediator_WhenNotifyingSchemaUpgraded_ThenPublishesNotificationWithDatabaseName()
    {
        IMediator mediator = Substitute.For<IMediator>();
        using var cts = new CancellationTokenSource();

        await mediator.NotifySchemaUpgradedAsync("MyDb", 4, true, cts.Token);

        await mediator.Received(1).PublishAsync(
            Arg.Is<SchemaUpgradedNotification>(n => n.DatabaseName == "MyDb" && n.Version == 4 && n.IsFullSchemaSnapshot),
            cts.Token);
    }
}
