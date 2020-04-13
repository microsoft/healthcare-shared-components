// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects
{
    public class TestDisposableObjectWithInterface : IEquatable<string>, IDisposable
    {
        public bool Equals(string other)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}