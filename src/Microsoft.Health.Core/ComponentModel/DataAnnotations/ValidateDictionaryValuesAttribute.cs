// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Health.Core.ComponentModel.DataAnnotations;

/// <summary>
/// Validates the data annotations of each value within a dictionary.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>ValidateEnumeratedItemsAttribute</c>, which would validate each <see cref="KeyValuePair{TKey, TValue}"/>,
/// this attribute validates the dictionary values themselves using <see cref="Validator"/>.
/// </para>
/// <para>
/// The attribute applies to any value implementing <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>
/// with exactly the specified type arguments, such as <see cref="IReadOnlyDictionary{TKey, TValue}"/>; other values are ignored.
/// </para>
/// <para>
/// <see langword="null"/> dictionaries are considered valid; combine with <see cref="RequiredAttribute"/> if necessary.
/// <see langword="null"/> values within the dictionary are considered invalid. Each failure is reported with the key of the
/// offending value, formatted using the invariant culture, such as <c>[key]: The Name field is required.</c>,
/// within a single <see cref="ValidationResult"/>.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
/// <typeparam name="TValue">The type of the dictionary values.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ValidateDictionaryValuesAttribute<TKey, TValue> : ValidationAttribute
    where TKey : notnull
    where TValue : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateDictionaryValuesAttribute{TKey, TValue}"/> class.
    /// </summary>
    public ValidateDictionaryValuesAttribute()
        : base("The {0} field contains one or more invalid values.")
    { }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // ValidationAttribute.IsValid(object) invokes this overload with a null context
        if (value is not IEnumerable<KeyValuePair<TKey, TValue>> entries)
            return ValidationResult.Success;

        List<string> errors = [];
        foreach ((TKey key, TValue entry) in entries)
        {
            if (entry is null)
            {
                errors.Add(string.Create(CultureInfo.InvariantCulture, $"[{key}]: The value is required."));
                continue;
            }

            List<ValidationResult> results = [];
            if (!Validator.TryValidateObject(entry, new ValidationContext(entry), results, validateAllProperties: true))
            {
                foreach (ValidationResult result in results)
                    errors.Add(string.Create(CultureInfo.InvariantCulture, $"[{key}]: {result.ErrorMessage}"));
            }
        }

        if (errors.Count == 0)
            return ValidationResult.Success;

        string message = FormatErrorMessage(validationContext?.DisplayName ?? string.Empty) + " " + string.Join(" ", errors);
        return validationContext?.MemberName is null
            ? new ValidationResult(message)
            : new ValidationResult(message, [validationContext.MemberName]);
    }
}
