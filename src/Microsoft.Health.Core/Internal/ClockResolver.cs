// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#if !NET8_0_OR_GREATER
using System;

namespace Microsoft.Health.Core.Internal;

/// <summary>
/// Not the clock you're looking for.
/// Used to override the static Clock class's UtcNowFunc for use in testing.
/// </summary>
public static class ClockResolver
{
    public static Func<DateTimeOffset> UtcNowFunc
    {
        get => Clock.UtcNowFunc;
        set => Clock.UtcNowFunc = value;
    }
}
#endif
