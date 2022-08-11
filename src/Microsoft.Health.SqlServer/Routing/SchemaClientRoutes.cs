// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.SqlServer.Features.Routing;

public static class SchemaClientRoutes
{
    private const string SchemaRoot = "_schema";
    private const string Compatibility = "compatibility";
    private const string Versions = "versions";
    private const string Current = Versions + "/current";
    private const string Script = "versions/{0}/script";
    private const string Diff = Script + "/diff";

    internal static readonly Uri RootedCurrentUri = new Uri("/" + SchemaRoot + "/" + Current, UriKind.Relative);
    internal static readonly Uri RootedCompatibilityUri = new Uri("/" + SchemaRoot + "/" + Compatibility, UriKind.Relative);
    internal static readonly Uri RootedVersionsUri = new Uri("/" + SchemaRoot + "/" + Versions, UriKind.Relative);

    public static Uri GetRootedScriptUri(int version) => GetFormattedUri(version, Script);
    public static Uri GetRootedDiffUri(int version) => GetFormattedUri(version, Diff);

    private static Uri GetFormattedUri(int version, string formatString)
    {
        var versionString = version.ToString(CultureInfo.CurrentCulture);
        var formattedScriptString = string.Format(CultureInfo.CurrentCulture, formatString, versionString);
        var relativeScriptUri = "/" + SchemaRoot + "/" + formattedScriptString;
        return new Uri(relativeScriptUri, UriKind.Relative);
    }
}
