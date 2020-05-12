// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public interface IScriptProvider
    {
        string GetMigrationScript(int version, bool applyFullSchemaSnapshot);

        Task<byte[]> GetSnapshotScriptAsBytesAsync(int version, CancellationToken cancellationToken);

        Task<byte[]> GetDiffScriptAsBytesAsync(int version, CancellationToken cancellationToken);
    }
}