// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides access to services with its associated metadata.
    /// </summary>
    /// <typeparam name="TServiceType">Type of the Service</typeparam>
    [SuppressMessage("Design", "CA1710", Justification = "This is a dictionary style design")]
    public interface IIndex<TServiceType> : IEnumerable<KeyValuePair<object, Lazy<TServiceType>>>
    {
        /// <summary>
        /// Gets the associated service by metadata
        /// </summary>
        /// <param name="metadata">Metadata associated with a service</param>
        TServiceType this[object metadata] { get; }

        /// <summary>
        /// Try get the service instance by using the metadata key
        /// </summary>
        /// <param name="metadata">Metadata associated with a service</param>
        /// <param name="value">Service instance</param>
        /// <returns>True if service was resolved</returns>
        bool TryGetValue(object metadata, out TServiceType value);
    }
}