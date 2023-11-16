// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Api;

internal static class SR
{
    private static readonly ResourceManager ResourceManager = new("Microsoft.Health.Api.Resources", typeof(SR).Assembly);

    public static string FailedHealthCheckMessage => ResourceManager.GetString(nameof(FailedHealthCheckMessage), CultureInfo.CurrentUICulture);

#if NET8_0_OR_GREATER
    public static CompositeFormat CustomAuditHeaderTooLarge { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(CustomAuditHeaderTooLarge), CultureInfo.CurrentUICulture));

    public static CompositeFormat DuplicateActionForAuditEvent { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(DuplicateActionForAuditEvent), CultureInfo.CurrentUICulture));

    public static CompositeFormat MissingAuditInformation { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(MissingAuditInformation), CultureInfo.CurrentUICulture));

    public static CompositeFormat PropertyCannotBeLargerThanAnother { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(PropertyCannotBeLargerThanAnother), CultureInfo.CurrentUICulture));

    public static CompositeFormat PropertyCannotBeLessThanValue { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(PropertyCannotBeLessThanValue), CultureInfo.CurrentUICulture));

    public static CompositeFormat TooManyCustomAuditHeaders { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(TooManyCustomAuditHeaders), CultureInfo.CurrentUICulture));
#else
    public static string CustomAuditHeaderTooLarge => ResourceManager.GetString(nameof(CustomAuditHeaderTooLarge), CultureInfo.CurrentUICulture);

    public static string DuplicateActionForAuditEvent => ResourceManager.GetString(nameof(DuplicateActionForAuditEvent), CultureInfo.CurrentUICulture);

    public static string MissingAuditInformation => ResourceManager.GetString(nameof(MissingAuditInformation), CultureInfo.CurrentUICulture);

    public static string PropertyCannotBeLargerThanAnother => ResourceManager.GetString(nameof(PropertyCannotBeLargerThanAnother), CultureInfo.CurrentUICulture);

    public static string PropertyCannotBeLessThanValue => ResourceManager.GetString(nameof(PropertyCannotBeLessThanValue), CultureInfo.CurrentUICulture);

    public static string TooManyCustomAuditHeaders => ResourceManager.GetString(nameof(TooManyCustomAuditHeaders), CultureInfo.CurrentUICulture);
#endif
}
