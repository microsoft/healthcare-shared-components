// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Api.Features.ByteCounter
{
    public class ByteCountingStreamFactory : IByteCountingStreamFactory
    {
        /// <inheritdoc/>
        public ByteCountingStream CreateByteCountingStream(Stream stream)
        {
            return new ByteCountingStream(stream);
        }
    }
}
