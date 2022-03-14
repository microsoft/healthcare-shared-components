// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Used in SqlClientRetryOptions to specify the retry mode
/// </summary>
public enum SqlRetryMode
{
    None,
    Exponential,
    Incremental,
    Fixed,
}
