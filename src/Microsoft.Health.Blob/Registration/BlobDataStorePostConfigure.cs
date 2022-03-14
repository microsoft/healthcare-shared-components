// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;

namespace Microsoft.Health.Blob.Registration;

internal class BlobDataStorePostConfigure : IPostConfigureOptions<BlobDataStoreConfiguration>
{
    public void PostConfigure(string name, BlobDataStoreConfiguration options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString) && options.AuthenticationType == BlobDataStoreAuthenticationType.ConnectionString)
        {
            options.ConnectionString = BlobLocalEmulator.ConnectionString;
        }
    }
}
