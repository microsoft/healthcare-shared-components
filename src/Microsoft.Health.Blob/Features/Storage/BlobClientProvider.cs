// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Core;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.Features.Storage
{
    public class BlobClientProvider : IHostedService, IRequireInitializationOnFirstRequest, IDisposable
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly RetryableInitializationOperation _initializationOperation;

        public BlobClientProvider(
            BlobDataStoreConfiguration blobDataStoreConfiguration,
            IBlobClientInitializer blobClientInitializer,
            ILogger<BlobClientProvider> logger,
            IEnumerable<IBlobContainerInitializer> collectionInitializers)
        {
            EnsureArg.IsNotNull(blobDataStoreConfiguration, nameof(blobDataStoreConfiguration));
            EnsureArg.IsNotNull(blobClientInitializer, nameof(blobClientInitializer));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(collectionInitializers, nameof(collectionInitializers));

            _blobServiceClient = blobClientInitializer.CreateBlobClient(blobDataStoreConfiguration);

            _initializationOperation = new RetryableInitializationOperation(
                () => blobClientInitializer.InitializeDataStoreAsync(_blobServiceClient, blobDataStoreConfiguration, collectionInitializers));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // The result is ignored and will be awaited in EnsureInitialized(). Exceptions are logged within DocumentClientInitializer.
            _ = _initializationOperation.EnsureInitialized();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Returns a task representing the initialization operation. Once completed,
        /// this method will always return a completed task. If the task fails, the method
        /// can be called again to retry the operation.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        public async Task EnsureInitialized() => await _initializationOperation.EnsureInitialized();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _initializationOperation.Dispose();
            }
        }

        public BlobServiceClient CreateBlobClient()
        {
            if (!_initializationOperation.IsInitialized)
            {
                _initializationOperation.EnsureInitialized().GetAwaiter().GetResult();
            }

            return _blobServiceClient;
        }
    }
}
