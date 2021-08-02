// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage
{
    public class BlobHostedServiceTests
    {
        private readonly IBlobInitializer _blobInitializer;
        private readonly List<IBlobContainerInitializer> _collectionInitializers;
        private readonly IOptions<BlobDataStoreConfiguration> _options;

        public BlobHostedServiceTests()
        {
            _blobInitializer = Substitute.For<IBlobInitializer>();

            _options = Substitute.For<IOptionsSnapshot<BlobDataStoreConfiguration>>();
            _options.Value.Returns(new BlobDataStoreConfiguration());

            _collectionInitializers = Substitute.For<List<IBlobContainerInitializer>>();
        }

        [Fact]
        public async void GivenCancellation_WhenStartingService_ThenInitializationIsCancelled()
        {
            _blobInitializer.InitializeDataStoreAsync(Arg.Any<List<IBlobContainerInitializer>>(), Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                });

            var blobHostedService = new BlobHostedService(_blobInitializer, _options, NullLogger<BlobHostedService>.Instance, _collectionInitializers);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => blobHostedService.StartAsync(cancellationTokenSource.Token));
        }
    }
}
