// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;

namespace Microsoft.Health.Abstractions.Features.Events
{
    /// <summary>
    /// Abstraction for the Azure.Messaging.EventGridPublisherClient
    /// </summary>
    public interface IEventGridPublisher
    {
        /// <summary>
        /// SendEventAsync
        /// </summary>
        /// <param name="eventGridEvent">EventGridEvent</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        public Task<Response> SendEventAsync(
            EventGridEvent eventGridEvent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// SendEventsAsync
        /// </summary>
        /// <param name="eventGridEvents">EventGridEvent</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        public Task<Response> SendEventsAsync(
            IEnumerable<EventGridEvent> eventGridEvents,
            CancellationToken cancellationToken = default);
    }
}
