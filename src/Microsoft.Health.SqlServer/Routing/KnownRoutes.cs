// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.SqlServer.Features.Routing;

public static class KnownRoutes
{
    public const string SchemaRoot = "_schema";

    public const string Compatibility = "compatibility";
    public const string Versions = "versions";

    public const string Current = Versions + "/current";
    public const string IdSegment = "{id:int}";
    public const string Script = Versions + "/" + IdSegment + "/script";
    public const string Diff = Script + "/diff";

    internal static readonly Uri RootedCurrentUri = new Uri("/" + SchemaRoot + "/" + Current, UriKind.Relative);
    internal static readonly Uri RootedCompatibilityUri = new Uri("/" + SchemaRoot + "/" + Compatibility, UriKind.Relative);
    internal static readonly Uri RootedVersionsUri = new Uri("/" + SchemaRoot + "/" + Versions, UriKind.Relative);

    internal static Uri RootedScriptUri(int version) => 
        new Uri("/" + SchemaRoot + "/" + Script.Replace(IdSegment, version.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase), UriKind.Relative);

    internal static Uri RootedDiffUri(int version) => 
        new Uri("/" + SchemaRoot + "/" + Diff.Replace(IdSegment, version.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase), UriKind.Relative);
}
