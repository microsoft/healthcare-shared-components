// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public interface IBaseSchemaRunner
{
    public Task EnsureBaseSchemaExistsAsync(CancellationToken cancellationToken);

    public Task EnsureInstanceSchemaRecordExistsAsync(CancellationToken cancellationToken);
}
