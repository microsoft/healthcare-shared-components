// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using DurableTask.AzureStorage;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class AzureStorageDurableTaskClientOptions
{
    [Required]
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    [Range(1, 16)]
    public int PartitionCount { get; set; } = 4;

    [Required]
    public string TaskHubName { get; set; } = "WorkerIntegrationTests";

    public AzureStorageOrchestrationServiceSettings ToOrchestrationServiceSettings()
    {
        return new()
        {
            PartitionCount = PartitionCount,
            TaskHubName = TaskHubName,
            StorageAccountClientProvider = new StorageAccountClientProvider(ConnectionString),
        };
    }
}
