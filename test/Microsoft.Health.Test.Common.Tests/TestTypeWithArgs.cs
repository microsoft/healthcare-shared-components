// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Test.Utilities.UnitTests;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal.", Justification = "The type must be public to mock.")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Type loaded via reflection.")]
[SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Type needs to be extensible for mocking.")]
public class TestTypeWithArgs
{
    public TestTypeWithArgs(TestType oneArg)
        : this(oneArg, null)
    {
    }

    public TestTypeWithArgs(TestType oneArg, string secondArg)
    {
        OneArg = oneArg;
        SecondArg = secondArg;
    }

    public TestType OneArg { get; }

    public string SecondArg { get; }
}
