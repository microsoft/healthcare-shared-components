// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests;

[TestClass]
public class ServiceControllerExtensionsTests
{
    [TestMethod]
    public void GivenAnAssembly_WhenScanningForModules_ThenNewModulesShouldBeLoaded()
    {
        var collection = Substitute.For<IServiceCollection>();
        collection.RegisterAssemblyModules(GetType().Assembly);
        collection.Received().Add(Arg.Is<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(TestComponent)));
    }

    [TestMethod]
    public void GivenANullAssembly_WhenScanningForModules_ThenAnArgumentExceptionShouldBeThrown()
    {
        var collection = Substitute.For<IServiceCollection>();
        Assert.Throws<ArgumentNullException>(() => collection.RegisterAssemblyModules(null));
    }
}
