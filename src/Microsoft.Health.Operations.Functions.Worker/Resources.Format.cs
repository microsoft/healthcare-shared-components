// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Operations.Functions.Worker;

internal static class FormatResources
{
#if NET8_0_OR_GREATER
    public static CompositeFormat InvalidInstanceId { get; } = CompositeFormat.Parse(Resources.InvalidInstanceId);
#else
    public static string InvalidInstanceId => Resources.InvalidInstanceId;
#endif
}

