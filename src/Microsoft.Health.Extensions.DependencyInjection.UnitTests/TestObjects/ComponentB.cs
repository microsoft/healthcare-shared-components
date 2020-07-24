// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects
{
    [DisplayName("Component B")]
    public class ComponentB : IComponent
    {
        public delegate IComponent Factory();

        public string Name { get; } = nameof(ComponentB);
    }
}