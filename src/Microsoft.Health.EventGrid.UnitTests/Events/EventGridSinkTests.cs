// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Azure.Messaging.EventGrid;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.EventGrid.UnitTests.Events
{
    /// <summary>
    /// EventGridSinkTests.
    /// </summary>
    public class EventGridSinkTests
    {
        private readonly EventGridSink _eventGridSink;
        private readonly IEventGridPublisher _publisher;
        private readonly EventGridEvent _testEventData;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSinkTests"/> class.
        /// </summary>
        public EventGridSinkTests()
        {
            _publisher = Substitute.For<IEventGridPublisher>();
            _eventGridSink = new EventGridSink(_publisher);
            _testEventData =
                new EventGridEvent(
                    "test",
                    "testEvent",
                    "1",
                    new BinaryData("testing"))
            {
                Topic = "Test Topic",
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow,
            };
        }

        /// <summary>
        /// Test Send Events.
        /// </summary>
        [Fact]
        public void TestSendEvents()
        {
            var eventsData = new List<EventGridEvent> { _testEventData };

            _ = _eventGridSink.WriteAsync(data: new ReadOnlyCollection<EventGridEvent>(list: eventsData));

            _ = _publisher
                .Received()
                .SendEventsAsync(
                    eventGridEvents: Arg.Is<IEnumerable<EventGridEvent>>(predicate: e =>
                        e != null &&
                        e.First().Id.Equals(_testEventData.Id) &&
                        e.First().EventTime.Equals(_testEventData.EventTime) &&
                        e.First().DataVersion.Equals(_testEventData.DataVersion) &&
                        e.First().EventType.Equals(_testEventData.EventType) &&
                        e.First().Subject.Equals(_testEventData.Subject) &&
                        e.First().Topic.Equals(_testEventData.Topic) &&
                        e.First().Data.ToString().Equals(_testEventData.Data.ToString())));
        }

        /// <summary>
        /// Test Send single Event
        /// </summary>
        [Fact]
        public void TestSendEvent()
        {
            _ = _eventGridSink.WriteAsync(_testEventData);

            _ = _publisher
                .Received()
                .SendEventAsync(
                    eventGridEvent: Arg.Is<EventGridEvent>(e =>
                        e != null &&
                        e.Id.Equals(_testEventData.Id) &&
                        e.EventTime.Equals(_testEventData.EventTime) &&
                        e.DataVersion.Equals(_testEventData.DataVersion) &&
                        e.EventType.Equals(_testEventData.EventType) &&
                        e.Subject.Equals(_testEventData.Subject) &&
                        e.Topic.Equals(_testEventData.Topic) &&
                        e.Data.ToString().Equals(_testEventData.Data.ToString())));
        }
    }
}
