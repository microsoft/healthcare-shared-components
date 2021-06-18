// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using EnsureThat;
using Microsoft.Health.Abstractions.Data;

namespace Microsoft.Health.EventGrid
{
    /// <summary>
    /// EventGridSink.
    /// </summary>
    public class EventGridSink : IChangeFeedSink<EventGridEvent>
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

        /// <inheritdoc />
        public async Task WriteAsync(EventGridEvent data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            await _eventGridPublisher.SendEventAsync(data).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WriteAsync(IReadOnlyCollection<EventGridEvent> data)
        {
            EnsureArg.IsNotNull(data, nameof(data));

            await _eventGridPublisher.SendEventsAsync(data).ConfigureAwait(false);
        }
    }
}
