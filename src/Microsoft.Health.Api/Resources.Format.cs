// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.Api;

internal static class FormatResources
{
#if NET8_0_OR_GREATER
    public static CompositeFormat CustomAuditHeaderTooLarge { get; } = CompositeFormat.Parse(Resources.CustomAuditHeaderTooLarge);

    public static CompositeFormat DuplicateActionForAuditEvent { get; } = CompositeFormat.Parse(Resources.DuplicateActionForAuditEvent);

    public static CompositeFormat MissingAuditInformation { get; } = CompositeFormat.Parse(Resources.MissingAuditInformation);

    public static CompositeFormat PropertyCannotBeLargerThanAnother { get; } = CompositeFormat.Parse(Resources.PropertyCannotBeLargerThanAnother);

    public static CompositeFormat PropertyCannotBeLessThanValue { get; } = CompositeFormat.Parse(Resources.PropertyCannotBeLessThanValue);

    public static CompositeFormat TooManyCustomAuditHeaders { get; } = CompositeFormat.Parse(Resources.TooManyCustomAuditHeaders);
#else
    public static string CustomAuditHeaderTooLarge => Resources.CustomAuditHeaderTooLarge;

    public static string DuplicateActionForAuditEvent => Resources.DuplicateActionForAuditEvent;

    public static string MissingAuditInformation => Resources.MissingAuditInformation;

    public static string PropertyCannotBeLargerThanAnother => Resources.PropertyCannotBeLargerThanAnother;

    public static string PropertyCannotBeLessThanValue => Resources.PropertyCannotBeLessThanValue;

    public static string TooManyCustomAuditHeaders => Resources.TooManyCustomAuditHeaders;
#endif
}
