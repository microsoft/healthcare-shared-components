// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Registration
{
    internal class TransportConfigure : IConfigureOptions<BlobClientOptions>
    {
        private readonly TransportOverrideOptions _desiredOptions;

        public TransportConfigure(TransportOverrideOptions options)
            => _desiredOptions = EnsureArg.IsNotNull(options, nameof(options));

        public void Configure(BlobClientOptions options)
            => options.Transport = _desiredOptions.Transport;
    }
}
