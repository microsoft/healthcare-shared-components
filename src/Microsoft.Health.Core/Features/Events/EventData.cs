// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Abstractions.Features.Events;

namespace Microsoft.Health.Core.Features.Events
{
    /// <summary>
    /// Event Data.
    /// </summary>
    public class EventData : IEvent
    {
        /// <inheritdoc />
        public string Topic { get; set; }

        /// <inheritdoc />
        public string Subject { get; set; }

        /// <inheritdoc />
        public string EventType { get; set; }

        /// <inheritdoc />
        public DateTimeOffset EventTime { get; set; }

        /// <inheritdoc />
        public string Id { get; set; }

        /// <inheritdoc />
        public string DataVersion { get; set; }

        /// <inheritdoc />
        public BinaryData Data { get; set; }
    }
}
