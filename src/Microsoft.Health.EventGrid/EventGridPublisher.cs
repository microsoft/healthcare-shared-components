// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Pipeline;
using Azure.Messaging.EventGrid;
using EnsureThat;

namespace Microsoft.Health.EventGrid
{
    /// <summary>
    /// EventGridPublisher.
    /// </summary>
    public class EventGridPublisher : IEventGridPublisher
    {
        private readonly EventGridPublisherClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher"/> class.
        /// </summary>
        /// <param name="endpoint">Uri of topic</param>
        /// <param name="key">access key</param>
        public EventGridPublisher(Uri endpoint, string key)
        {
            EnsureArg.IsNotNull(endpoint, nameof(endpoint));
            EnsureArg.IsNotNull(key, nameof(key));

            _client = new EventGridPublisherClient(endpoint, new AzureKeyCredential(key));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisher"/> class.
        /// </summary>
        /// <param name="endpoint">Uri</param>
        /// <param name="httpClient">httpClient with client certificate</param>
        /// <param name="keyCredentialName">string</param>
        public EventGridPublisher(Uri endpoint, HttpClient httpClient, string keyCredentialName)
        {
            EnsureArg.IsNotNull(endpoint, nameof(endpoint));
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));
            EnsureArg.IsNotNull(keyCredentialName, nameof(keyCredentialName));

            var options = new EventGridPublisherClientOptions
            {
                Transport = new HttpClientTransport(httpClient),
            };
            _client = new EventGridPublisherClient(endpoint, new AzureKeyCredential(keyCredentialName), options);
        }

        /// <inheritdoc />
        public async Task<Response> SendEventAsync(
            EventGridEvent eventGridEvent,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(eventGridEvent, nameof(eventGridEvent));

            return await _client.SendEventAsync(eventGridEvent, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Response> SendEventsAsync(
            IEnumerable<EventGridEvent> eventGridEvents,
            CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(eventGridEvents, nameof(eventGridEvents));

            return await _client.SendEventsAsync(eventGridEvents, cancellationToken).ConfigureAwait(false);
        }
    }
}
