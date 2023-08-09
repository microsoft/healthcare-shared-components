// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Keys;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.CustomerManagedKey.Configs;
using Microsoft.Health.CustomerManagedKey.Health;

namespace Microsoft.Health.Blob.UnitTests.Features.Health;

internal sealed class TestBlobHealthCheck : BlobHealthCheck
{
    public const string TestBlobHealthCheckName = "TestBlobHealthCheck";

    public TestBlobHealthCheck(
        KeyClient keyClient,
        BlobServiceClient client,
        IOptions<CustomerManagedKeyOptions> cmkOptions,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        string containerConfigurationName,
        IKeyTestProvider keyTestProvider,
        IBlobClientTestProvider testProvider,
        ILogger<TestBlobHealthCheck> logger)
        : base(
              keyClient,
              client,
              cmkOptions,
              namedBlobContainerConfigurationAccessor,
              TestBlobHealthCheckName,
              keyTestProvider,
              testProvider,
              logger)
    {
    }
}
