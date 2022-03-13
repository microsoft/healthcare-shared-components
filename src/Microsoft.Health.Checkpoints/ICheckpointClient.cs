// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Checkpoints;

public interface ICheckpointClient
{
    Task<ICheckpoint> GetCheckpointAsync(string partition, string checkpointIdentifier, CancellationToken token = default);

    Task<ICheckpoint> SetCheckpointAsync(ICheckpoint checkpoint, CancellationToken token = default);
}
