// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.Health.Core.UnitTests.Features.Security.Samples;

[Flags]
internal enum DataActions
{
    [EnumMember(Value = "none")]
    None = 0,

    [EnumMember(Value = "read")]
    Read = 1,

    [EnumMember(Value = "write")]
    Write = 1 << 1,

    [EnumMember(Value = "delete")]
    Delete = 1 << 2,

    [EnumMember(Value = "*")]
    All = (Delete << 1) - 1,
}
