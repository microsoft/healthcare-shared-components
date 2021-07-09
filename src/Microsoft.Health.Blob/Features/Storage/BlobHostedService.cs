// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Microsoft.Health.Blob.Features.Storage
{
    /// <summary>
    /// Called from the webHost startup. Will make sure the Blob containers are initialized before the webservice is ready
    /// </summary>
    public class BlobHostedService : IHostedService
    {
        private readonly IBlobInitializer _blobInitializer;
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;
        private readonly IEnumerable<IBlobContainerInitializer> _collectionInitializers;

        public BlobHostedService(
            IBlobInitializer blobInitializer,
            IOptions<BlobDataStoreConfiguration> blobDataStoreConfigurationOption,
            ILogger<BlobHostedService> logger,
            IEnumerable<IBlobContainerInitializer> collectionInitializers)
        {
            EnsureArg.IsNotNull(blobInitializer, nameof(blobInitializer));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(collectionInitializers, nameof(collectionInitializers));
            EnsureArg.IsNotNull(blobDataStoreConfigurationOption?.Value, nameof(blobDataStoreConfigurationOption));

            _blobInitializer = blobInitializer;
            _blobDataStoreConfiguration = blobDataStoreConfigurationOption.Value;
            _collectionInitializers = collectionInitializers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var sleepTime = TimeSpan.FromSeconds(_blobDataStoreConfiguration.RequestOptions.InitialConnectWaitBeforeRetryInSeconds);
            var waitForReadyTimeout = TimeSpan.FromMinutes(_blobDataStoreConfiguration.RequestOptions.InitialConnectMaxWaitInMinutes);

            // Handle RBAC propogation delays for compute identity to talk to storage account
            AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(waitForReadyTimeout);
            AsyncRetryPolicy retryPolicy = Policy.Handle<Azure.RequestFailedException>(exp => exp.Status == 403).WaitAndRetryForeverAsync(_ => sleepTime);

            await timeoutPolicy
                   .WrapAsync(retryPolicy)
                   .ExecuteAsync((token) => _blobInitializer.InitializeDataStoreAsync(_collectionInitializers), cancellationToken)
                   .ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
