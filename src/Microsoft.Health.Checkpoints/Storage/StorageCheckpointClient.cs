// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Checkpoints.Storage
{
    public class StorageCheckpointClient : ICheckpointClient
    {
        private BlobContainerClient _storageClient;
        private ILogger<StorageCheckpointClient> _logger;
        private const string _lastProcessedDateTime = "LastProcessedDateTime";
        private const string _lastProcessedIdentifier = "LastProcessedIdentifier";

        public StorageCheckpointClient(BlobContainerClient containerClient, ILogger<StorageCheckpointClient> logger)
        {
            EnsureArg.IsNotNull(containerClient);

            _storageClient = containerClient;
            _logger = logger;
        }

        public virtual async Task<ICheckpoint> GetCheckpointAsync(string partition, string checkpointIdentifier, CancellationToken token = default)
        {
            EnsureArg.IsNotNullOrEmpty(partition, nameof(partition));
            EnsureArg.IsNotNullOrEmpty(checkpointIdentifier, nameof(checkpointIdentifier));

            var checkpoint = new Checkpoint();
            var blobName = GetBlobName(partition, checkpointIdentifier);

            IAsyncEnumerable<Page<BlobItem>> resultSegment = _storageClient.GetBlobsAsync(
                                    traits: BlobTraits.Metadata,
                                    states: BlobStates.All,
                                    prefix: blobName,
                                    cancellationToken: token)
                                    .AsPages();

            await foreach (Page<BlobItem> blobPage in resultSegment)
            {
                if (blobPage.Values.Count == 0)
                {
                    _logger.LogInformation("No blob found for blob name {blobName}.", blobName);
                    return null;
                }

                if (blobPage.Values.Count > 1)
                {
                    throw new StorageCheckpointClientException($"Multiple blobs found for blob name {blobName}");
                }

                foreach (BlobItem blobItem in blobPage.Values)
                {
                    string lastProcessedDateTime;
                    string lastProcessedIdentifier;
                    if (blobItem.Metadata.TryGetValue(_lastProcessedDateTime, out lastProcessedDateTime))
                    {
                        DateTimeOffset.TryParse(lastProcessedDateTime, null, DateTimeStyles.RoundtripKind, out DateTimeOffset lastEventTimestamp);
                        checkpoint.LastProcessedDateTime = lastEventTimestamp;
                    }

                    if (blobItem.Metadata.TryGetValue(_lastProcessedIdentifier, out lastProcessedIdentifier))
                    {
                        checkpoint.LastProcessedIdentifier = lastProcessedIdentifier;
                    }

                    if (string.IsNullOrWhiteSpace(lastProcessedDateTime) && string.IsNullOrWhiteSpace(lastProcessedIdentifier))
                    {
                        _logger.LogInformation("No valid checkpoint found for {blobName}.", blobName);
                        return null;
                    }

                    checkpoint.ETag = blobItem.Properties.ETag.ToString();
                    checkpoint.Identifier = checkpointIdentifier;
                    checkpoint.Partition = partition;
                }
            }

            return checkpoint;
        }

        public virtual async Task<ICheckpoint> SetCheckpointAsync(ICheckpoint checkpoint, CancellationToken token = default)
        {
            EnsureArg.IsNotNull(checkpoint);
            EnsureArg.IsNotNullOrWhiteSpace(checkpoint.Partition);
            EnsureArg.IsNotNullOrWhiteSpace(checkpoint.Identifier);

            var lastProcessedDateTime = checkpoint.LastProcessedDateTime.ToString("o");
            var lastProcessedIdentifier = checkpoint.LastProcessedIdentifier;

            var blobName = GetBlobName(checkpoint.Partition, checkpoint.Identifier);
            var blobClient = _storageClient.GetBlobClient(blobName);

            var metadata = new Dictionary<string, string>()
            {
                { _lastProcessedDateTime,  lastProcessedDateTime },
                { _lastProcessedIdentifier,  lastProcessedIdentifier },
            };

            try
            {
                var blobRequestOptions = new BlobRequestConditions() { IfMatch = new ETag(checkpoint.ETag) };
                BlobInfo result = await blobClient.SetMetadataAsync(metadata, blobRequestOptions, token);
                checkpoint.ETag = result.ETag.ToString();
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                throw new StorageCheckpointClientException($"The checkpoint {blobName} ETag does not match ETag provided");
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound || ex.ErrorCode == BlobErrorCode.ContainerNotFound)
            {
                using (var blobContent = new MemoryStream(Array.Empty<byte>()))
                {
                    BlobContentInfo result = await blobClient.UploadAsync(blobContent, metadata: metadata, cancellationToken: token).ConfigureAwait(false);
                    checkpoint.ETag = result.ETag.ToString();
                }
            }

            return checkpoint;
        }

        private static string GetBlobName(string partition, string identifier)
        {
            return $"{partition}/{identifier}";
        }
    }
}
