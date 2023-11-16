// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
#if NET8_0_OR_GREATER
using System.Text;
#endif

namespace Microsoft.Health.SqlServer;

internal static class SR
{
    public static readonly ResourceManager ResourceManager = new ResourceManager("Microsoft.Health.SqlServer.Resources", typeof(SR).Assembly);

    public static string AvailableVersionsDefaultErrorMessage => ResourceManager.GetString(nameof(AvailableVersionsDefaultErrorMessage), CultureInfo.CurrentUICulture);

    public static string AvailableVersionsErrorMessage => ResourceManager.GetString(nameof(AvailableVersionsErrorMessage), CultureInfo.CurrentUICulture);

    public static string BaseScriptNotFound => ResourceManager.GetString(nameof(BaseScriptNotFound), CultureInfo.CurrentUICulture);

    public static string CompatibilityDefaultErrorMessage => ResourceManager.GetString(nameof(CompatibilityDefaultErrorMessage), CultureInfo.CurrentUICulture);

    public static string CompatibilityRecordNotFound => ResourceManager.GetString(nameof(CompatibilityRecordNotFound), CultureInfo.CurrentUICulture);

    public static string CurrentSchemaVersionStoredProcedureNotFound => ResourceManager.GetString(nameof(CurrentSchemaVersionStoredProcedureNotFound), CultureInfo.CurrentUICulture);

    public static string InstanceSchemaRecordErrorMessage => ResourceManager.GetString(nameof(InstanceSchemaRecordErrorMessage), CultureInfo.CurrentUICulture);

    public static string InstanceSchemaRecordTableNotFound => ResourceManager.GetString(nameof(InstanceSchemaRecordTableNotFound), CultureInfo.CurrentUICulture);

    public static string InsufficientDatabasePermissionsMessage => ResourceManager.GetString(nameof(InsufficientDatabasePermissionsMessage), CultureInfo.CurrentUICulture);

    public static string InsufficientTablesPermissionsMessage => ResourceManager.GetString(nameof(InsufficientTablesPermissionsMessage), CultureInfo.CurrentUICulture);

    public static string OperationFailed => ResourceManager.GetString(nameof(OperationFailed), CultureInfo.CurrentUICulture);

    public static string ScriptNotFound => ResourceManager.GetString(nameof(ScriptNotFound), CultureInfo.CurrentUICulture);

#if NET8_0_OR_GREATER
    public static CompositeFormat CurrentDefaultErrorDescription { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(CurrentDefaultErrorDescription), CultureInfo.CurrentUICulture));

    public static CompositeFormat DecimalValueOutOfRange { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(DecimalValueOutOfRange), CultureInfo.CurrentUICulture));

    public static CompositeFormat InvalidDatabaseIdentifier { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(InvalidDatabaseIdentifier), CultureInfo.CurrentUICulture));

    public static CompositeFormat InvalidVersionMessage { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(InvalidVersionMessage), CultureInfo.CurrentUICulture));

    public static CompositeFormat PrecisionValueOutOfRange { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(PrecisionValueOutOfRange), CultureInfo.CurrentUICulture));

    public static CompositeFormat SpecifiedVersionNotAvailable { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(SpecifiedVersionNotAvailable), CultureInfo.CurrentUICulture));

    public static CompositeFormat StringTooLong { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(StringTooLong), CultureInfo.CurrentUICulture));

    public static CompositeFormat VersionIncompatibilityMessage { get; } = CompositeFormat.Parse(ResourceManager.GetString(nameof(VersionIncompatibilityMessage), CultureInfo.CurrentUICulture));
#else
    public static string CurrentDefaultErrorDescription => ResourceManager.GetString(nameof(CurrentDefaultErrorDescription), CultureInfo.CurrentUICulture);

    public static string DecimalValueOutOfRange => ResourceManager.GetString(nameof(DecimalValueOutOfRange), CultureInfo.CurrentUICulture);

    public static string InvalidDatabaseIdentifier => ResourceManager.GetString(nameof(InvalidDatabaseIdentifier), CultureInfo.CurrentUICulture);

    public static string InvalidVersionMessage => ResourceManager.GetString(nameof(InvalidVersionMessage), CultureInfo.CurrentUICulture);

    public static string PrecisionValueOutOfRange => ResourceManager.GetString(nameof(PrecisionValueOutOfRange), CultureInfo.CurrentUICulture);

    public static string SpecifiedVersionNotAvailable => ResourceManager.GetString(nameof(SpecifiedVersionNotAvailable), CultureInfo.CurrentUICulture);

    public static string StringTooLong => ResourceManager.GetString(nameof(StringTooLong), CultureInfo.CurrentUICulture);

    public static string VersionIncompatibilityMessage => ResourceManager.GetString(nameof(VersionIncompatibilityMessage), CultureInfo.CurrentUICulture);
#endif
}
