// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Core;

internal static class FormatResources
{
#if NET8_0_OR_GREATER
    public static CompositeFormat DuplicateRoleNames { get; } = CompositeFormat.Parse(Resources.DuplicateRoleNames);

    public static CompositeFormat ErrorValidatingRoles { get; } = CompositeFormat.Parse(Resources.ErrorValidatingRoles);
#else
    public static string DuplicateRoleNames => Resources.DuplicateRoleNames;

    public static string ErrorValidatingRoles => Resources.ErrorValidatingRoles;
#endif
}
