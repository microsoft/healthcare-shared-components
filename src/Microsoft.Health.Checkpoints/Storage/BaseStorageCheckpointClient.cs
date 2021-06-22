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
    public class BaseStorageCheckpointClient : ICheckpointClient
    {
        private BlobContainerClient _storageClient;
        private ILogger<BaseStorageCheckpointClient> _logger;
        private const string _lastProcessedDateTime = "LastProcessedDateTime";
        private const string _lastProcessedIdentifier = "LastProcessedIdentifier";

        public BaseStorageCheckpointClient(BlobContainerClient containerClient, ILogger<BaseStorageCheckpointClient> logger)
        {
            EnsureArg.IsNotNull(containerClient);

            _storageClient = containerClient;
            _logger = logger;
        }

        public virtual async Task<ICheckpoint> GetCheckpointAsync(string checkpointIdentifier)
        {
            EnsureArg.IsNotNullOrEmpty(checkpointIdentifier, nameof(checkpointIdentifier));

            ICheckpoint checkpoint = new Checkpoint();

            try
            {
                var resultSegment = _storageClient.GetBlobsAsync(
                                        traits: BlobTraits.Metadata,
                                        states: BlobStates.All,
                                        prefix: checkpointIdentifier,
                                        cancellationToken: CancellationToken.None)
                                        .AsPages();

                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    if (blobPage.Values.Count == 0)
                    {
                        _logger.LogWarning($"No blob found for identifier {checkpointIdentifier}");
                    }

                    if (blobPage.Values.Count > 1)
                    {
                        _logger.LogWarning($"Multiple blobs found for identifier {checkpointIdentifier}");
                    }

                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        DateTimeOffset lastEventTimestamp = DateTime.MinValue;

                        if (blobItem.Metadata.TryGetValue(_lastProcessedDateTime, out var dt))
                        {
                            DateTimeOffset.TryParse(dt, null, DateTimeStyles.AssumeUniversal, out lastEventTimestamp);
                            checkpoint.LastProcessedDateTime = lastEventTimestamp;
                        }

                        if (blobItem.Metadata.TryGetValue(_lastProcessedIdentifier, out var id))
                        {
                            checkpoint.LastProcessedIdentifier = id;
                        }

                        if (checkpoint.LastProcessedDateTime == DateTime.MinValue || checkpoint.LastProcessedIdentifier == null)
                        {
                            _logger.LogWarning($"No valid checkpoint found for {checkpointIdentifier}. Using default checkpoint of {checkpoint.LastProcessedDateTime}");
                        }

                        checkpoint.ETag = (ETag)blobItem.Properties.ETag;
                        checkpoint.Identifier = checkpointIdentifier;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

            return checkpoint;
        }

        public virtual async Task<ICheckpoint> SetCheckpointAsync(ICheckpoint checkpoint)
        {
            EnsureArg.IsNotNull(checkpoint);
            EnsureArg.IsNotNullOrWhiteSpace(checkpoint.Identifier);

            var lastProcessedDateTime = EnsureArg.IsNotNullOrWhiteSpace(checkpoint.LastProcessedDateTime.DateTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), nameof(checkpoint.LastProcessedDateTime));
            var lastProcessedIdentifier = checkpoint.LastProcessedIdentifier;

            var blobName = checkpoint.Identifier;
            var blobClient = _storageClient.GetBlobClient(blobName);

            var metadata = new Dictionary<string, string>()
            {
                { _lastProcessedDateTime,  lastProcessedDateTime },
                { _lastProcessedIdentifier,  lastProcessedIdentifier },
            };

            try
            {
                var blobRequestOptions = new BlobRequestConditions();
                blobRequestOptions.IfMatch = checkpoint.ETag;
                BlobInfo result = await blobClient.SetMetadataAsync(metadata, blobRequestOptions);
                checkpoint.ETag = result.ETag;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                _logger.LogWarning("The checkpoint's ETag does not match ETag provided.");
            }
            catch (RequestFailedException ex) when ((ex.ErrorCode == BlobErrorCode.BlobNotFound) || (ex.ErrorCode == BlobErrorCode.ContainerNotFound))
            {
                using (var blobContent = new MemoryStream(Array.Empty<byte>()))
                {
                    await blobClient.UploadAsync(blobContent, metadata: metadata).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return checkpoint;
        }
    }
}
