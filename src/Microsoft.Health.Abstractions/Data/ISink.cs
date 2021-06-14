// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Health.Abstractions.Data
{
    /// <summary>
    /// Generic Sink for writing data to any destination.
    /// </summary>
    /// <typeparam name="T">Generic data type T to write</typeparam>
    public interface ISink<in T>
    {
        /// <summary>
        /// Write data to sink.
        /// </summary>
        /// <param name="data">Data of type T.</param>
        /// <returns>Task.</returns>
        Task WriteAsync(T data);

        /// <summary>
        /// WRite a collection of type T.
        /// </summary>
        /// <param name="data">Data of type T.</param>
        /// <returns>Task.</returns>
        Task WriteAsync(IReadOnlyCollection<T> data);
    }
}
