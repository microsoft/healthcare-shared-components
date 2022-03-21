// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects;

public sealed class TestDisposableObjectWithInterface : IEquatable<string>, IDisposable
{
    public bool Equals(string other)
        => false;

    public void Dispose()
    { }
}
