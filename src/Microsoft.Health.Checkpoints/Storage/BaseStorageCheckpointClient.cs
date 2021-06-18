// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnsureThat;

namespace Microsoft.Health.Checkpoints.Storage
{
    public class BaseStorageCheckpointClient : ICheckpointClient
    {
        private BlobContainerClient _storageClient;

        public BaseStorageCheckpointClient(BlobContainerClient containerClient)
        {
            EnsureArg.IsNotNull(containerClient);

            _storageClient = containerClient;
        }

        public virtual Task<ICheckpoint> GetCheckpointAsync(string checkpointIdentifier)
        {
            Task<ICheckpoint> GetCheckpointAsync()
            {
                ICheckpoint checkpoint = new Checkpoint();

                foreach (BlobItem blob in _storageClient.GetBlobs(traits: BlobTraits.Metadata, states: BlobStates.All, prefix: checkpointIdentifier, cancellationToken: CancellationToken.None))
                {
                    DateTimeOffset lastEventTimestamp = DateTime.MinValue;

                    if (blob.Metadata.TryGetValue("LastProcessedDt", out var dt))
                    {
                        DateTimeOffset.TryParse(dt, null, DateTimeStyles.AssumeUniversal, out lastEventTimestamp);
                        checkpoint.LastProcessedDt = lastEventTimestamp;
                    }

                    if (blob.Metadata.TryGetValue("LastProcessedId", out var id))
                    {
                        checkpoint.LastProcessedId = id;
                    }

                    checkpoint.Identifier = checkpointIdentifier;
                }

                return Task.FromResult(checkpoint);
            }

            return GetCheckpointAsync();
        }

        public virtual async Task SetCheckpointAsync(ICheckpoint checkpoint)
        {
            EnsureArg.IsNotNull(checkpoint);
            EnsureArg.IsNotNullOrWhiteSpace(checkpoint.Identifier);

            var lastProcessedDt = EnsureArg.IsNotNullOrWhiteSpace(checkpoint.LastProcessedDt.DateTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), nameof(checkpoint.LastProcessedDt));
            var lastProcessedId = checkpoint.LastProcessedId;

            var blobName = checkpoint.Identifier;
            var blobClient = _storageClient.GetBlobClient(blobName);

            var metadata = new Dictionary<string, string>()
            {
                { "LastProcessedDt",  lastProcessedDt },
                { "LastProcessedId",  lastProcessedId },
            };

            try
            {
                await blobClient.SetMetadataAsync(metadata);
            }
            catch (RequestFailedException ex) when ((ex.ErrorCode == BlobErrorCode.BlobNotFound) || (ex.ErrorCode == BlobErrorCode.ContainerNotFound))
            {
                using (var blobContent = new MemoryStream(Array.Empty<byte>()))
                {
                    await blobClient.UploadAsync(blobContent, metadata: metadata).ConfigureAwait(false);
                }
            }
        }
    }
}
