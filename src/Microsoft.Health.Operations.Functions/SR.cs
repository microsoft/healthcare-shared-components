// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Operations.Functions;

internal static class SR
{
    private static readonly ResourceManager ResourceManager = new ResourceManager("Microsoft.Health.Operations.Functions.Resources", typeof(SR).Assembly);

#if NET8_0_OR_GREATER
    public static CompositeFormat InvalidInstanceId { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(InvalidInstanceId), CultureInfo.CurrentUICulture)!);
#else
    public static string InvalidInstanceId => ResourceManager.GetString(nameof(InvalidInstanceId), CultureInfo.CurrentUICulture)!;
#endif
}
