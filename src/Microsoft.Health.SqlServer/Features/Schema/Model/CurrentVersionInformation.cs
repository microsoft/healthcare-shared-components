// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

public class CurrentVersionInformation
{
    public CurrentVersionInformation(int id, SchemaVersionStatus status, IList<string> servers)
    {
        Id = id;
        Status = status;
        Servers = servers;
    }

    public int Id { get; }

    public SchemaVersionStatus Status { get; }

    public IList<string> Servers { get; }
}
