// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Core;

internal static class SR
{
    private static readonly ResourceManager ResourceManager = new ResourceManager("Microsoft.Health.Core.Resources", typeof(SR).Assembly);

    public static string CustomHeaderPrefixCannotBeEmpty => ResourceManager.GetString(nameof(CustomHeaderPrefixCannotBeEmpty), CultureInfo.CurrentUICulture)!;

#if NET8_0_OR_GREATER
    public static CompositeFormat DuplicateRoleNames { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(DuplicateRoleNames), CultureInfo.CurrentUICulture)!);

    public static CompositeFormat ErrorValidatingRoles { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(ErrorValidatingRoles), CultureInfo.CurrentUICulture)!);
#else
    public static string DuplicateRoleNames => ResourceManager.GetString(nameof(DuplicateRoleNames), CultureInfo.CurrentUICulture)!;

    public static string ErrorValidatingRoles => ResourceManager.GetString(nameof(ErrorValidatingRoles), CultureInfo.CurrentUICulture)!;
#endif
}
