// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class FunctionWorkerOptions
{
    public const string DefaultSectionName = "FunctionWorkerTest";

    [Range(1, int.MaxValue)]
    public int Port { get; set; } = 7071;

    [Required]
    public AzureStorageDurableTaskClientOptions DurableTask { get; set; } = default!;
}
