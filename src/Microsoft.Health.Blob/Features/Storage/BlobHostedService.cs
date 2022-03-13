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

namespace Microsoft.Health.Blob.Features.Storage;

/// <summary>
/// Called from the webHost startup. Will make sure the Blob containers are initialized before the webservice is ready
/// </summary>
public class BlobHostedService : IHostedService
{
    private readonly IBlobInitializer _blobInitializer;
    private readonly ILogger<BlobHostedService> _logger;
    private readonly BlobInitializerOptions _options;
    private readonly IEnumerable<IBlobContainerInitializer> _collectionInitializers;

    public BlobHostedService(
        IBlobInitializer blobInitializer,
        IEnumerable<IBlobContainerInitializer> collectionInitializers,
        IOptions<BlobInitializerOptions> options,
        ILogger<BlobHostedService> logger)
    {
        _blobInitializer = EnsureArg.IsNotNull(blobInitializer, nameof(blobInitializer));
        _collectionInitializers = EnsureArg.IsNotNull(collectionInitializers, nameof(collectionInitializers));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Handle RBAC propogation delays for compute identity to talk to storage account
        TimeSpan retryDelay = _options.RetryDelay;
        AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(_options.Timeout);
        AsyncRetryPolicy retryPolicy = Policy.Handle<Azure.RequestFailedException>(exp => exp.Status == 403).WaitAndRetryForeverAsync(_ => retryDelay);

        await timeoutPolicy
            .WrapAsync(retryPolicy)
            .ExecuteAsync((token) => _blobInitializer.InitializeDataStoreAsync(_collectionInitializers, token), cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Blob containers initialized");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
