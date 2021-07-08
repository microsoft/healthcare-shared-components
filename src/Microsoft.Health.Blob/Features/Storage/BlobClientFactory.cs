// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    internal static class BlobClientFactory
    {
        public static BlobServiceClient Create(BlobDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            // Configure the blob client default request options and retry logic
            var blobClientOptions = new BlobClientOptions();
            blobClientOptions.Retry.MaxRetries = configuration.RequestOptions.ExponentialRetryMaxAttempts;
            blobClientOptions.Retry.Mode = Azure.Core.RetryMode.Exponential;
            blobClientOptions.Retry.Delay = TimeSpan.FromSeconds(configuration.RequestOptions.ExponentialRetryBackoffDeltaInSeconds);
            blobClientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(configuration.RequestOptions.ServerTimeoutInMinutes);

            if (configuration.AuthenticationType == BlobDataStoreAuthenticationType.ManagedIdentity)
            {
                var defaultCredentials = new DefaultAzureCredential();
                return new BlobServiceClient(new Uri(configuration.ConnectionString), defaultCredentials, blobClientOptions);
            }

            return new BlobServiceClient(configuration.ConnectionString, blobClientOptions);
        }
    }
}
