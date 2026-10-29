// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Configs;

internal static class DataAnnotationsValidator
{
    public static bool TryValidate(object instance, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }

    public static ValidationResult AssertSingleFailure(object instance, string memberName)
    {
        Assert.IsFalse(TryValidate(instance, out List<ValidationResult> results));

        ValidationResult result = Assert.ContainsSingle(results);
        Assert.AreEqual(memberName, Assert.ContainsSingle(result.MemberNames), result.ErrorMessage);
        return result;
    }
}
