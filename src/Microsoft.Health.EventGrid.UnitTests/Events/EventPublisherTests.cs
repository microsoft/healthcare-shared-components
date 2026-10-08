// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.EventGrid.UnitTests.Events;

[TestClass]
public class EventPublisherTests
{
    /// <summary>
    /// TestCreateEventPublisherWithNullEndPoint.
    /// </summary>
    [TestMethod]
    public void CreateEventPublisherWithNullEndPoint_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(null, "some key"));
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(null, new HttpClient(), "some key"));
    }

    /// <summary>
    /// TestCreateEventPublisherWithNullAccessKey.
    /// </summary>
    [TestMethod]
    public void CreateEventPublisherWithNullAccessKey_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events"), key: null));
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events"), credential: null));
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events"), new HttpClient(), null));
    }

    /// <summary>
    /// Test CreateEventPublisherWithNull httpClient.
    /// </summary>
    [TestMethod]
    public void CreateEventPublisherWithNullHttpClient_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventGridPublisher(new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events"), null, "some key name"));
    }
}
