// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;

namespace Microsoft.Health.Core;

internal static class FormatResources
{
    public static CompositeFormat DuplicateRoleNames { get; } = CompositeFormat.Parse(Resources.DuplicateRoleNames);

    public static CompositeFormat ErrorValidatingRoles { get; } = CompositeFormat.Parse(Resources.ErrorValidatingRoles);
}
