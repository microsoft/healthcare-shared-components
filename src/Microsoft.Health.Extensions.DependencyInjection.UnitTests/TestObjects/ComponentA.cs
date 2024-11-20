// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This type is activated via a service provider.")]
internal sealed class ComponentA : IComponent
{
    public string Name { get; } = nameof(ComponentA);
}
