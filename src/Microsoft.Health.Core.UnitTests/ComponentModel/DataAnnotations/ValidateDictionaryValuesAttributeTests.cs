// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Health.Core.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Core.UnitTests.ComponentModel.DataAnnotations;

[TestClass]
public class ValidateDictionaryValuesAttributeTests
{
    private const string Prefix = "The Items field contains one or more invalid values.";

    [TestMethod]
    public void GivenNullDictionary_WhenValidating_ThenSucceed()
    {
        Assert.IsEmpty(Validate(new Container { Items = null }));
    }

    [TestMethod]
    public void GivenEmptyDictionary_WhenValidating_ThenSucceed()
    {
        Assert.IsEmpty(Validate(new Container { Items = new Dictionary<string, Item>() }));
    }

    [TestMethod]
    public void GivenValidValues_WhenValidating_ThenSucceed()
    {
        var container = new Container
        {
            Items = new Dictionary<string, Item>
            {
                ["a"] = new Item { Name = "A" },
                ["b"] = new Item { Name = "B", Count = 10 },
            },
        };

        Assert.IsEmpty(Validate(container));
    }

    [TestMethod]
    public void GivenInvalidValue_WhenValidating_ThenReportOnlyInvalidKey()
    {
        var container = new Container
        {
            Items = new Dictionary<string, Item>
            {
                ["a"] = new Item { Name = "A" },
                ["b"] = new Item { Name = null },
            },
        };

        ValidationResult result = Assert.ContainsSingle(Validate(container));

        Assert.AreEqual(nameof(Container.Items), Assert.ContainsSingle(result.MemberNames));
        Assert.AreEqual($"{Prefix} [b]: The Name field is required.", result.ErrorMessage);
    }

    [TestMethod]
    public void GivenMultipleInvalidValues_WhenValidating_ThenReportAllErrorsInSingleResult()
    {
        var container = new Container
        {
            Items = new Dictionary<string, Item>
            {
                ["a"] = new Item { Name = null, Count = 0 },
                ["b"] = new Item { Name = "B", Count = 11 },
            },
        };

        ValidationResult result = Assert.ContainsSingle(Validate(container));

        Assert.StartsWith(Prefix, result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("[a]: The Name field is required.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("[a]: The field Count must be between 1 and 10.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("[b]: The field Count must be between 1 and 10.", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenNullValue_WhenValidating_ThenFail()
    {
        var container = new Container { Items = new Dictionary<string, Item> { ["a"] = null } };

        ValidationResult result = Assert.ContainsSingle(Validate(container));

        Assert.AreEqual($"{Prefix} [a]: The value is required.", result.ErrorMessage);
    }

    [TestMethod]
    public void GivenOtherDictionaryImplementations_WhenValidating_ThenValidateValues()
    {
        var invalid = new Dictionary<string, Item> { ["a"] = new Item { Name = null } };

        Assert.ContainsSingle(Validate(new Container { Items = new ReadOnlyDictionary<string, Item>(invalid) }));
        Assert.ContainsSingle(Validate(new Container { Items = invalid.ToImmutableDictionary() }));
        Assert.ContainsSingle(Validate(new Container { Items = invalid.ToImmutableSortedDictionary() }));
    }

    [TestMethod]
    public void GivenDisplayName_WhenValidating_ThenUseDisplayNameInMessage()
    {
        var container = new DisplayContainer { Items = new Dictionary<string, Item> { ["a"] = new Item { Name = null } } };

        ValidationResult result = Assert.ContainsSingle(Validate(container));

        Assert.AreEqual(nameof(DisplayContainer.Items), Assert.ContainsSingle(result.MemberNames));
        Assert.StartsWith("The Inventory field contains one or more invalid values.", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenNoMemberName_WhenValidating_ThenReturnResultWithoutMemberNames()
    {
        var attribute = new ValidateDictionaryValuesAttribute<string, Item>();
        var items = new Dictionary<string, Item> { ["a"] = new Item { Name = null } };

        ValidationResult result = attribute.GetValidationResult(items, new ValidationContext(items));

        Assert.IsNotNull(result);
        Assert.IsEmpty(result.MemberNames);
        Assert.Contains("[a]: The Name field is required.", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenNoValidationContext_WhenCheckingIsValid_ThenReturnResult()
    {
        var attribute = new ValidateDictionaryValuesAttribute<string, Item>();

        Assert.IsTrue(attribute.IsValid(new Dictionary<string, Item> { ["a"] = new Item { Name = "A" } }));
        Assert.IsFalse(attribute.IsValid(new Dictionary<string, Item> { ["a"] = new Item { Name = null } }));
        Assert.IsFalse(attribute.IsValid(new Dictionary<string, Item> { ["a"] = null }));
    }

    [TestMethod]
    public void GivenNonStringKeys_WhenValidating_ThenReportKeys()
    {
        var attribute = new ValidateDictionaryValuesAttribute<int, Item>();
        var items = new Dictionary<int, Item> { [1] = new Item { Name = "A" }, [2] = new Item { Name = null } };

        ValidationResult result = attribute.GetValidationResult(items, new ValidationContext(items));

        Assert.IsNotNull(result);
        Assert.Contains("[2]: The Name field is required.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("[1]", result.ErrorMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenFormattableKeys_WhenValidating_ThenFormatKeysWithInvariantCulture()
    {
        var attribute = new ValidateDictionaryValuesAttribute<double, Item>();
        var items = new Dictionary<double, Item> { [1.5] = new Item { Name = null } };

        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            ValidationResult result = attribute.GetValidationResult(items, new ValidationContext(items));

            Assert.IsNotNull(result);
            Assert.Contains("[1.5]: The Name field is required.", result.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void GivenNonMatchingType_WhenValidating_ThenIgnoreValue()
    {
        var attribute = new ValidateDictionaryValuesAttribute<string, Item>();
        var items = new Dictionary<int, Item> { [1] = new Item { Name = null } };

        Assert.IsTrue(attribute.IsValid(items));
        Assert.IsTrue(attribute.IsValid(new Dictionary<string, Container> { ["a"] = new Container() }));
        Assert.IsTrue(attribute.IsValid("not a dictionary"));
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }

    private sealed class Item
    {
        [Required]
        public string Name { get; set; }

        [Range(1, 10)]
        public int Count { get; set; } = 1;
    }

    private sealed class Container
    {
        [ValidateDictionaryValues<string, Item>]
        public IReadOnlyDictionary<string, Item> Items { get; set; }
    }

    private sealed class DisplayContainer
    {
        [Display(Name = "Inventory")]
        [ValidateDictionaryValues<string, Item>]
        public IReadOnlyDictionary<string, Item> Items { get; set; }
    }
}
