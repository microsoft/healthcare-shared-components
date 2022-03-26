// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Test.Utilities.UnitTests;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Type metadata is read via reflection.")]
public class TestType
{
    public string Property1 { get; set; }

    internal static string StaticProperty { get; set; } = "Initial";

    public string CallMe()
    {
        return "hello";
    }
}
