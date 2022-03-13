// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

/// <summary>
/// Represents a SQL index.
/// </summary>
public class Index
{
    public Index(string indexName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(indexName, nameof(indexName));

        IndexName = indexName;
    }

    public string IndexName { get; }

    public static implicit operator string(Index i)
    {
        EnsureArg.IsNotNull(i, nameof(i));
        return i.ToString();
    }

    public override string ToString() => IndexName;
}
