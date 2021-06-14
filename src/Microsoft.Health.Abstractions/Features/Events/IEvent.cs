// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Abstractions.Features.Events
{
    /// <summary>
    /// Common Event interface.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets or sets the resource path of the event source. This must be set when publishing
        ///     the event to a domain, and must not be set when publishing the event to a topic.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>Gets or sets a resource path relative to the topic path.</summary>
        public string Subject { get; set; }

        /// <summary>Gets or sets the type of the event that occurred.</summary>
        public string EventType { get; set; }

        /// <summary>Gets or sets the time (in UTC) the event was generated.</summary>
        public DateTimeOffset EventTime { get; set; }

        /// <summary>
        /// Gets or sets a unique identifier for the event.
        /// </summary>
        public string Id { get; set; }

        /// <summary>Gets or sets the schema version of the data object.</summary>
        public string DataVersion { get; set; }

        /// <summary>
        /// Gets or sets the Data.
        /// </summary>
        public BinaryData Data { get; set; }
    }
}
