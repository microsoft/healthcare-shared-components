// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public interface ISchemaManagerDataStore
    {
        Task ExecuteScriptAndCompleteSchemaVersionAsync(string script, int version, CancellationToken cancellationToken);

        Task DeleteSchemaVersionAsync(int version, string status, CancellationToken cancellationToken);

        Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken);

        Task ExecuteScriptAsync(string script, CancellationToken cancellationToken);

        Task<bool> BaseSchemaExistsAsync(CancellationToken cancellationToken);

        Task<bool> InstanceSchemaRecordExistsAsync(CancellationToken cancellationToken);
    }
}
