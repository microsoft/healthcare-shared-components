// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection.UnitTests.TestObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests;

[TestClass]
public class TypeRegistrationTests
{
    private readonly ServiceCollection _collection;

    public TypeRegistrationTests()
    {
        _collection = new ServiceCollection();
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringTransientAsSelf_ThenTheServicesIsRegistered()
    {
        new TypeRegistration(_collection, typeof(string))
            .Transient()
            .AsSelf();

        Assert.AreEqual(typeof(string), _collection.Single().ServiceType);
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringTransientAsSelf_ThenTheServiceGivesNewInstances()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Transient()
            .AsSelf();

        var ioc = _collection.BuildServiceProvider();

        Assert.AreNotEqual(ioc.GetService<List<string>>().GetHashCode(), ioc.GetService<List<string>>().GetHashCode());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringTransientAsSelfAsService_ThenTheServicesIsRegistered()
    {
        new TypeRegistration(_collection, typeof(StreamReader))
            .Transient()
            .AsSelf()
            .AsService<TextReader>();

        Assert.AreEqual(typeof(StreamReader), _collection.First().ServiceType);
        Assert.AreEqual(typeof(TextReader), _collection.Skip(1).First().ServiceType);
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringAndReplacingService_ThenTheNewServicesIsRegistered()
    {
        new TypeRegistration(_collection, typeof(StreamReader))
            .Transient()
            .AsService<TextReader>();

        new TypeRegistration(_collection, typeof(StringReader))
            .Transient()
            .ReplaceService<TextReader>();

        foreach (ServiceDescriptor d in _collection)
        {
            Assert.AreEqual(typeof(StringReader), d.ImplementationType);
            Assert.AreEqual(typeof(TextReader), d.ServiceType);
        }
    }

    [TestMethod]
    public void GivenAFactory_WhenReplacingSelf_ThenOnlyTheNewServiceIsRegistered()
    {
        _collection
            .Add(sp => "a")
            .Transient()
            .AsSelf();

        _collection
            .Add(sp => "b")
            .Transient()
            .ReplaceSelf();

        Assert.ContainsSingle(_collection.BuildServiceProvider().GetService<IEnumerable<string>>(), "b");
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringTransientAsSelf_ThenTheServicesIsRegistered()
    {
        new TypeRegistration(_collection, typeof(StreamReader), provider => new StreamReader(new MemoryStream()))
            .Transient()
            .AsSelf()
            .AsService<TextReader>();

        Assert.AreEqual(typeof(StreamReader), _collection.First().ServiceType);
        Assert.AreEqual(typeof(TextReader), _collection.Skip(1).First().ServiceType);
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringTransientAsImplementedInterfaces_ThenIDisposableIsNotRegistered()
    {
        new TypeRegistration(_collection, typeof(TestDisposableObjectWithInterface))
            .Transient()
            .AsImplementedInterfaces();

        Assert.IsTrue(_collection.All(x => x.ServiceType != typeof(IDisposable)));
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringTransientAsImplementedInterfaces_ThenTheServicesIsRegistered()
    {
        new TypeRegistration(_collection, typeof(TestDisposableObjectWithInterface))
            .Transient()
            .AsImplementedInterfaces();

        Assert.AreEqual(typeof(IEquatable<string>), _collection.Single().ServiceType);
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringTransientAsImplementedInterfaces_ThenTheServicesWithDelegateIsRegistered()
    {
        new TypeRegistration(_collection, typeof(TestDisposableObjectWithInterface))
            .Transient()
            .AsImplementedInterfaces(x => typeof(IEquatable<string>).IsAssignableFrom(x));

        Assert.AreEqual(typeof(IEquatable<string>), _collection.Single().ServiceType);
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringScopedAsSelfAsServices_ThenTheSameInstanceIsResolvedForBoth()
    {
        new TypeRegistration(_collection, typeof(List<string>), provider => new List<string> { Guid.NewGuid().ToString() })
            .Scoped()
            .AsSelf()
            .AsService<IList<string>>()
            .AsImplementedInterfaces();

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<List<string>>();
        var b = ioc.GetService<IList<string>>();
        var c = ioc.GetService<IEnumerable<string>>();

        Assert.AreEqual(a.First(), b.First());
        Assert.AreEqual(a.First(), c.First());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringScopedAsSelfAsServices_ThenTheSameInstanceIsResolvedForBoth()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Scoped()
            .AsSelf()
            .AsService<IList<string>>();

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<List<string>>();
        var b = ioc.GetService<IList<string>>();

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void GivenADelegate_WhenRegisteringSingletonAsSelfAsServices_ThenTheSameInstanceIsResolvedForBoth()
    {
        new TypeRegistration(_collection, typeof(List<string>), provider => new List<string> { Guid.NewGuid().ToString() })
            .Singleton()
            .AsSelf()
            .AsService<IList<string>>()
            .AsImplementedInterfaces();

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<List<string>>();
        var b = ioc.GetService<IList<string>>();
        var c = ioc.GetService<IEnumerable<string>>();

        Assert.AreEqual(a.First(), b.First());
        Assert.AreEqual(a.First(), c.First());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringSingletonAsSelfAsServices_ThenTheSameInstanceIsResolvedForBoth()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Singleton()
            .AsSelf()
            .AsService<IList<string>>();

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<List<string>>();
        var b = ioc.GetService<IList<string>>();

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringLazy_ThenTypeCanBeResolved()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Singleton()
            .AsSelf()
            .AsService<IList<string>>();

        _collection.AddTransient(typeof(Lazy<>), typeof(LazyProvider<>));

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<Lazy<List<string>>>();
        var b = ioc.GetService<Lazy<IList<string>>>();

        Assert.AreEqual(a.Value.GetHashCode(), b.Value.GetHashCode());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringAFactory_ThenTypeCanBeResolved()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Singleton()
            .AsSelf()
            .AsService<IList<string>>()
            .AsFactory<List<string>>()
            .AsFactory<IList<string>>();

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<Func<List<string>>>();
        var b = ioc.GetService<Func<IList<string>>>();

        Assert.AreEqual(a().GetHashCode(), b().GetHashCode());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringOwned_ThenTypeCanBeResolvedInDifferentScope()
    {
        new TypeRegistration(_collection, typeof(List<string>))
            .Scoped()
            .AsSelf()
            .AsService<IList<string>>();

        _collection.AddTransient(typeof(IScoped<>), typeof(Scoped<>));

        var ioc = _collection.BuildServiceProvider();

        var a = ioc.GetService<IScoped<List<string>>>();
        var b = ioc.GetService<IScoped<IList<string>>>();

        Assert.AreNotEqual(a.Value.GetHashCode(), b.Value.GetHashCode());
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringScopedWithExplicitRegistration_ThenOnlyExplicitRegistrationIsReturned()
    {
        // Add open generic first
        _collection.AddScoped();

        // Adds actual List
        _collection.Add<List<string>>()
            .Transient()
            .AsService<IList<string>>();

        // Adds explicit Scope registration
        _collection.Add(x => new TestScope(x.GetService<IList<string>>()))
            .Scoped()
            .AsService<IScoped<IList<string>>>();

        var ioc = _collection.BuildServiceProvider();

        var resolvedService = ioc.GetService<IScoped<IList<string>>>();

        Assert.IsExactInstanceOfType<TestScope>(resolvedService);
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringScopedWithExplicitRegistrationReverseOrder_ThenOnlyExplicitRegistrationIsReturned()
    {
        // Adds actual List
        _collection.Add<List<string>>()
            .Transient()
            .AsService<IList<string>>();

        // Adds explicit Scope registration
        _collection.Add(x => new TestScope(x.GetService<IList<string>>()))
            .Scoped()
            .AsService<IScoped<IList<string>>>();

        // Add open generic second
        _collection.AddScoped();

        var ioc = _collection.BuildServiceProvider();

        var resolvedService = ioc.GetService<IScoped<IList<string>>>();

        Assert.IsExactInstanceOfType<TestScope>(resolvedService);
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringAsSelf_ThenTheTypeAppearsAsMetadataOnTheServiceDescriptor()
    {
        _collection.Add<List<string>>()
            .Transient()
            .AsSelf();

        Assert.AreEqual(typeof(List<string>), (_collection.Single() as ServiceDescriptorWithMetadata)?.Metadata);
    }

    [TestMethod]
    public void GivenAType_WhenRegisteringAsSelfAndAsAnotherService_ThenTheTypeAppearsAsMetadataOnTheServiceDescriptor()
    {
        _collection.Add<List<string>>()
            .Transient()
            .AsSelf()
            .AsService<IList<string>>();

        foreach (ServiceDescriptor d in _collection)
            Assert.AreEqual(typeof(List<string>), (d as ServiceDescriptorWithMetadata)?.Metadata);
    }

    [TestMethod]
    public void GivenFactory_WhenRegisteringAsSelfAsAService_ThenTheTypeAppearsAsMetadataOnTheServiceDescriptor()
    {
        _collection.Add(sp => new List<string>())
            .Transient()
            .AsService<IList<string>>();

        foreach (ServiceDescriptor d in _collection)
            Assert.AreEqual(typeof(List<string>), (d as ServiceDescriptorWithMetadata)?.Metadata);
    }

    [TestMethod]
    public void GivenADelegate_WhenResolvingComponent_ThenResolverReturnsRegisteredService()
    {
        _collection.Add<ComponentA>()
            .Transient()
            .AsSelf()
            .AsService<IComponent>();

        _collection.Add<ComponentB>()
            .Transient()
            .AsSelf();

        _collection.AddDelegate<ComponentB.Factory, ComponentB>();

        var provider = _collection.BuildServiceProvider();

        var componentFactory = provider.GetService<ComponentB.Factory>();
        IComponent instance = componentFactory.Invoke();

        Assert.IsExactInstanceOfType<ComponentB>(instance);
    }

    [TestMethod]
    public void GivenADelegateFromTypeBuilder_WhenResolvingComponent_ThenResolverReturnsRegisteredService()
    {
        _collection.Add<ComponentA>()
            .Transient()
            .AsSelf()
            .AsService<IComponent>();

        _collection.Add<ComponentB>()
            .Transient()
            .AsSelf()
            .AsService<IComponent>()
            .AsDelegate<ComponentB.Factory>();

        var provider = _collection.BuildServiceProvider();

        // Using Func<IComponent> won't work here because there are 2 components that implement this interface.
        // Using the delegate ComponentB.Factory works to resolve the desired instance while maintaining the interface
        var componentFactory = provider.GetService<ComponentB.Factory>();
        IComponent instance = componentFactory.Invoke();

        Assert.IsExactInstanceOfType<ComponentB>(instance);
    }

    [TestMethod]
    public void GivenADelegateWithIncompatibleType_WhenResolvingComponent_ThenExceptionIsThrown()
    {
        Assert.Throws<InvalidOperationException>(_collection.AddDelegate<ComponentB.Factory, int>);
    }
}
