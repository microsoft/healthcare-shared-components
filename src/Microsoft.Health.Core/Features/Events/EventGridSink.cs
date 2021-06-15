// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using EnsureThat;
using Microsoft.Health.Abstractions.Data;
using Microsoft.Health.Abstractions.Features.Events;

namespace Microsoft.Health.Core.Features.Events
{
    /// <summary>
    /// EventGridSink.
    /// </summary>
    /// <typeparam name="T">T of Type IEvent.</typeparam>
    public class EventGridSink<T> : ISink<T>
        where T : class, IEvent
    {
        private readonly IEventGridPublisher _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSink{T}"/> class.
        /// </summary>
        /// <param name="publisher">IEventGridPublisher</param>
        public EventGridSink(IEventGridPublisher publisher)
        {
            EnsureArg.IsNotNull(publisher, nameof(publisher));
            _client = publisher;
        }

        private async Task SendEventAsync(EventGridEvent eventGridEvent)
        {
            await _client.SendEventAsync(eventGridEvent);
        }

        private async Task SendEventsAsync(IEnumerable<EventGridEvent> eventGridEvents)
        {
           await _client.SendEventsAsync(eventGridEvents);
        }

        /// <inheritdoc />
        public async Task WriteAsync(T data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            var eventGridEvent = new EventGridEvent(data.Subject, data.EventType, data.DataVersion, data.Data)
            {
                Topic = data.Topic, EventTime = data.EventTime, Id = data.Id,
            };

            await SendEventAsync(eventGridEvent);
        }

        /// <inheritdoc />
        public async Task WriteAsync(IReadOnlyCollection<T> data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            var events = data.Select(item =>
                new EventGridEvent(item.Subject, item.EventType, item.DataVersion, item.Data)
                {
                    Topic = item.Topic, EventTime = item.EventTime, Id = item.Id,
                }).ToList();

            await SendEventsAsync(events);
        }
    }
}
