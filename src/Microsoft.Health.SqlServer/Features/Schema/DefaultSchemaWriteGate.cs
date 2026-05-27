// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Default <see cref="ISchemaWriteGate"/> implementation that always permits writes.
/// Used when geo-replication is not configured.
/// </summary>
internal sealed class DefaultSchemaWriteGate : ISchemaWriteGate
{
}
