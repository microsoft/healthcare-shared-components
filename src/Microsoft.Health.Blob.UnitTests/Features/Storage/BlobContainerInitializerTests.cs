// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage;

public class BlobContainerInitializerTests
{
    private const string TestContainerName = "testcontainer1";
    private readonly NullLogger<BlobContainerInitializer> _logger;
    private readonly BlobServiceClient _blobClient;

    public BlobContainerInitializerTests()
    {
        _logger = new NullLogger<BlobContainerInitializer>();

        var blobContainerClient = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());
        blobContainerClient.CreateIfNotExistsAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Substitute.For<Task<Response<BlobContainerInfo>>>();
            });

        _blobClient = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
        _blobClient.GetBlobContainerClient(TestContainerName).Returns(blobContainerClient);
    }

    [Fact]
    public async Task GivenCancelation_WhenInitializingContainer_ThenOperationCanceledExceptionIsThrown()
    {
        var blobContainerInitializer = new BlobContainerInitializer(TestContainerName, _logger);
        using var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => blobContainerInitializer.InitializeContainerAsync(_blobClient, cancellationTokenSource.Token));
    }
}
