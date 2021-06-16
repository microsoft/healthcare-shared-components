// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
    public class EventGridSink : ISink<EventGridEvent>
    {
        private readonly IEventGridPublisher _eventGridPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSink"/> class.
        /// </summary>
        /// <param name="publisher">IEventGridPublisher</param>
        public EventGridSink(IEventGridPublisher publisher)
        {
            EnsureArg.IsNotNull(publisher, nameof(publisher));
            _eventGridPublisher = publisher;
        }

        private async Task SendEventAsync(EventGridEvent eventGridEvent)
        {
            await _eventGridPublisher.SendEventAsync(eventGridEvent).ConfigureAwait(false);
        }

        private async Task SendEventsAsync(IEnumerable<EventGridEvent> eventGridEvents)
        {
           await _eventGridPublisher.SendEventsAsync(eventGridEvents).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WriteAsync(EventGridEvent data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            await SendEventAsync(data).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WriteAsync(IReadOnlyCollection<EventGridEvent> data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            await SendEventsAsync(data).ConfigureAwait(false);
        }
    }
}
