// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Api.Features.ByteCounter
{
    public interface IByteCountingStreamFactory
    {
        /// <summary>
        /// Creates a <see cref="ByteCountingStream"/>
        /// </summary>
        /// <param name="stream">An underlying stream.</param>
        /// <returns>A byte counting stream that wraps the specified underlying stream.</returns>
        ByteCountingStream CreateByteCountingStream(Stream stream);
    }
}
