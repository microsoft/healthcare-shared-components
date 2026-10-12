// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Test.Utilities.UnitTests;

[TestClass]
public class MockTests
{
    [TestMethod]
    public void GivenAnInstance_WhenMockingAProperty_ThenThePropertyIsMockedAndReset()
    {
        var magic = "magic";
        var newvalue = "newValue";

        var p = new TestType
        {
            Property1 = magic,
        };

        using (Mock.Property(() => p.Property1, newvalue))
        {
            Assert.AreEqual(newvalue, p.Property1);
        }

        Assert.AreEqual(magic, p.Property1);
    }

    [TestMethod]
    public void GivenAStatic_WhenMockingAProperty_ThenThePropertyIsMockedAndReset()
    {
        var initial = "Initial";
        var newvalue = "newValue";

        Assert.AreEqual(initial, TestType.StaticProperty);

        using (Mock.Property(() => TestType.StaticProperty, newvalue))
        {
            Assert.AreEqual(newvalue, TestType.StaticProperty);
        }

        Assert.AreEqual(initial, TestType.StaticProperty);
    }

    [TestMethod]
    public void GivenAnInstance_WhenMockingAMethod_ThenANotSupportedExceptionIsThrown()
    {
        var p = new TestType();

        Assert.Throws<NotSupportedException>(() => Mock.Property(() => p.CallMe(), "test"));
    }

    [TestMethod]
    public void GivenAType_WhenMockingAnInstance_TheConstructorWithLeastArgumentsIsUsed()
    {
        var instance = Mock.TypeWithArguments<TestTypeWithArgs>();

        Assert.IsNotNull(instance);
        Assert.IsNotNull(instance.OneArg);
        Assert.IsNull(instance.SecondArg);
    }

    [TestMethod]
    public void GivenAType_WhenMockingAnInstance_ParametersCanBeUsed()
    {
        var parameter = new TestType();
        var instance = Mock.TypeWithArguments<TestTypeWithArgs>(parameter);

        Assert.AreEqual(parameter, instance.OneArg);
    }

    [TestMethod]
    public void GivenAType_WhenMockingAnInstance_ParameterWithDerivedTypeCanBeUsed()
    {
        var parameter = new DerivedTestType();
        var instance = Mock.TypeWithArguments<TestTypeWithArgs>(parameter);

        Assert.AreEqual(parameter, instance.OneArg);
    }

    private sealed class DerivedTestType : TestType
    {
    }
}
