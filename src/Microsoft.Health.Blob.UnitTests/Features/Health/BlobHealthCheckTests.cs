// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Core.Features.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Health;

public class BlobHealthCheckTests
{
    private readonly IStoragePrerequisiteHealthCheckPublisher _storagePrerequisiteHealthCheckPublisher = Substitute.For<IStoragePrerequisiteHealthCheckPublisher>();
    private readonly BlobServiceClient _client = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
    private readonly IBlobClientTestProvider _testProvider = Substitute.For<IBlobClientTestProvider>();
    private readonly BlobContainerConfiguration _containerConfiguration = new BlobContainerConfiguration { ContainerName = "mycont" };

    private readonly TestBlobHealthCheck _healthCheck;

    public BlobHealthCheckTests()
    {
        IOptionsSnapshot<BlobContainerConfiguration> optionsSnapshot = Substitute.For<IOptionsSnapshot<BlobContainerConfiguration>>();
        optionsSnapshot.Get(TestBlobHealthCheck.TestBlobHealthCheckName).Returns(_containerConfiguration);

        _testProvider.PerformTestAsync(Arg.Any<BlobServiceClient>(), Arg.Any<BlobContainerConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        var entries = new Dictionary<string, HealthReportEntry>()
        {
            { "report1", new HealthReportEntry(HealthStatus.Healthy, string.Empty, TimeSpan.FromSeconds(1), null, null) }
        };
        _storagePrerequisiteHealthCheckPublisher.HealthReport.Returns(new HealthReport(entries, TimeSpan.FromSeconds(1)));

        _healthCheck = new TestBlobHealthCheck(
            _client,
            optionsSnapshot,
            TestBlobHealthCheck.TestBlobHealthCheckName,
            _testProvider,
            _storagePrerequisiteHealthCheckPublisher,
            NullLogger<TestBlobHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenBlobDataStoreIsAvailableAndKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeReturned()
    {
        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task GivenBlobDataStoreIsNotAvailable_WhenHealthIsChecked_ThenExceptionIsThrown()
    {
        _testProvider.PerformTestAsync(default, _containerConfiguration).ThrowsForAnyArgs<HttpRequestException>();

        await Assert.ThrowsAsync<HttpRequestException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext())).ConfigureAwait(false);
    }

    [Theory]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy)]
    public async Task GivenPrerequisiteIsNotHealthy_WhenHealthIsChecked_ThenNotHealthyStatusReturned(HealthStatus healthStatus)
    {
        var entries = new Dictionary<string, HealthReportEntry>()
        {
            { "report1", new HealthReportEntry(healthStatus, "Prereq unhealthy", TimeSpan.FromSeconds(1), null, null) }
        };
        _storagePrerequisiteHealthCheckPublisher.HealthReport.Returns(new HealthReport(entries, TimeSpan.FromSeconds(1)));

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        Assert.Equal(healthStatus, result.Status);
    }

    [Fact]
    public async Task GivenCancellation_WhenHealthIsChecked_ThenOperationCancelledExceptionIsThrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationTokenSource.Token)).ConfigureAwait(false);
    }
}
