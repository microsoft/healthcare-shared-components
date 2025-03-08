// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;

namespace Microsoft.Health.Api;

internal static class FormatResources
{
    public static CompositeFormat CustomAuditHeaderTooLarge { get; } = CompositeFormat.Parse(Resources.CustomAuditHeaderTooLarge);

    public static CompositeFormat DuplicateActionForAuditEvent { get; } = CompositeFormat.Parse(Resources.DuplicateActionForAuditEvent);

    public static CompositeFormat MissingAuditInformation { get; } = CompositeFormat.Parse(Resources.MissingAuditInformation);

    public static CompositeFormat PropertyCannotBeLargerThanAnother { get; } = CompositeFormat.Parse(Resources.PropertyCannotBeLargerThanAnother);

    public static CompositeFormat PropertyCannotBeLessThanValue { get; } = CompositeFormat.Parse(Resources.PropertyCannotBeLessThanValue);

    public static CompositeFormat TooManyCustomAuditHeaders { get; } = CompositeFormat.Parse(Resources.TooManyCustomAuditHeaders);
}
