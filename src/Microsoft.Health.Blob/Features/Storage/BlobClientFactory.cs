// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage;

internal static class BlobClientFactory
{
    public static BlobServiceClient Create(BlobDataStoreConfiguration configuration)
    {
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        // Configure the blob client default request options and retry logic
        BlobClientOptions blobClientOptions = new()
        {
            Retry =
            {
                MaxRetries = configuration.RequestOptions.ExponentialRetryMaxAttempts,
                Mode = Azure.Core.RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(configuration.RequestOptions.ExponentialRetryBackoffDeltaInSeconds),
                NetworkTimeout = TimeSpan.FromMinutes(configuration.RequestOptions.ServerTimeoutInMinutes),
            }
        };

        if (configuration.AuthenticationType == BlobDataStoreAuthenticationType.ManagedIdentity)
        {
            ManagedIdentityCredential credential = new(configuration.ManagedIdentityClientId, configuration.Credentials);
            return new BlobServiceClient(new Uri(configuration.ConnectionString), credential, blobClientOptions);
        }

        return new BlobServiceClient(configuration.ConnectionString, blobClientOptions);
    }
}
