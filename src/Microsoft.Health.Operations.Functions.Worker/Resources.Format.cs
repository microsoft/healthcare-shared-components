// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;

namespace Microsoft.Health.Operations.Functions.Worker;

internal static class FormatResources
{
    public static CompositeFormat InvalidInstanceId { get; } = CompositeFormat.Parse(Resources.InvalidInstanceId);
}

