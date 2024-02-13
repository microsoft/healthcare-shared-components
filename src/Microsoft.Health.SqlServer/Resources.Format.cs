// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.SqlServer;

internal static class FormatResources
{
#if NET8_0_OR_GREATER
    public static CompositeFormat CurrentDefaultErrorDescription { get; } = CompositeFormat.Parse(Resources.CurrentDefaultErrorDescription);

    public static CompositeFormat DecimalValueOutOfRange { get; } = CompositeFormat.Parse(Resources.DecimalValueOutOfRange);

    public static CompositeFormat InvalidDatabaseIdentifier { get; } = CompositeFormat.Parse(Resources.InvalidDatabaseIdentifier);

    public static CompositeFormat InvalidVersionMessage { get; } = CompositeFormat.Parse(Resources.InvalidVersionMessage);

    public static CompositeFormat PrecisionValueOutOfRange { get; } = CompositeFormat.Parse(Resources.PrecisionValueOutOfRange);

    public static CompositeFormat SpecifiedVersionNotAvailable { get; } = CompositeFormat.Parse(Resources.SpecifiedVersionNotAvailable);

    public static CompositeFormat StringTooLong { get; } = CompositeFormat.Parse(Resources.StringTooLong);

    public static CompositeFormat VersionIncompatibilityMessage { get; } = CompositeFormat.Parse(Resources.VersionIncompatibilityMessage);
#else
    public static string CurrentDefaultErrorDescription => Resources.CurrentDefaultErrorDescription;

    public static string DecimalValueOutOfRange => Resources.DecimalValueOutOfRange;

    public static string InvalidDatabaseIdentifier => Resources.InvalidDatabaseIdentifier;

    public static string InvalidVersionMessage => Resources.InvalidVersionMessage;

    public static string PrecisionValueOutOfRange => Resources.PrecisionValueOutOfRange;

    public static string SpecifiedVersionNotAvailable => Resources.SpecifiedVersionNotAvailable;

    public static string StringTooLong => Resources.StringTooLong;

    public static string VersionIncompatibilityMessage => Resources.VersionIncompatibilityMessage;
#endif
}
