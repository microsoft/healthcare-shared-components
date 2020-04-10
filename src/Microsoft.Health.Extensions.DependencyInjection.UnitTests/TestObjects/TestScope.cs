// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects
{
    public class TestScope : IScoped<IList<string>>
    {
        public TestScope(IList<string> value)
        {
            Value = value;
        }

        public IList<string> Value { get; }

        public void Dispose()
        {
        }
    }
}