// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using Microsoft.Health.Core.Features.Events;
using Xunit;

namespace Microsoft.Health.Core.UnitTests.Features.Events
{
    /// <summary>
    /// EventPublisherTests.
    /// </summary>
    public class EventPublisherTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisherTests"/> class.
        /// </summary>
        public EventPublisherTests()
        {
        }

        /// <summary>
        /// TestCreateEventPublisherWithNullEndPoint.
        /// </summary>
        [Fact]
        public void CreateEventPublisherWithNullEndPoint_ShowThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var eventGridPublisher = new EventGridPublisher(null, "some key");
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var eventGridPublisher = new EventGridPublisher(null, new HttpClient(new HttpClientHandler()), "some key");
            });
        }

        /// <summary>
        /// TestCreateEventPublisherWithNullAccessKey.
        /// </summary>
        [Fact]
        public void CreateEventPublisherWithNullAccessKey_ShowThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var testTopicEndPoint = new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events");
                var eventGridPublisher = new EventGridPublisher(testTopicEndPoint, null);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                var testTopicEndPoint = new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events");
                var eventGridPublisher = new EventGridPublisher(testTopicEndPoint, new HttpClient(new HttpClientHandler()), null);
            });
        }

        /// <summary>
        /// Test CreateEventPublisherWithNull httpClient.
        /// </summary>
        [Fact]
        public void CreateEventPublisherWithNullHttpClient_ShowThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var testTopicEndPoint = new Uri("https://microsoft-healthcareapis-workspaces.westus2-1.eventgrid-int.azure.net/eventGrid/api/events");
                var eventGridPublisher = new EventGridPublisher(testTopicEndPoint, null, "some key name");
            });
        }
    }
}
