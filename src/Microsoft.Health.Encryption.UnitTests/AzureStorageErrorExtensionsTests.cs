// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;
using Microsoft.Health.Encryption.Customer.Extensions;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Health.Encryption.UnitTests;

public class AzureStorageErrorExtensionsTests
{
    public static IEnumerable<object[]> GetExceptionToResultMapping
    {
        get
        {
            yield return new object[] { new RequestFailedException(403, "The key vault key is not found to unwrap the encryption key.", "KeyVaultEncryptionKeyNotFound", innerException: null), true };
            yield return new object[] { new RequestFailedException(403, "The key vault is not found for encryption.", "KeyVaultVaultNotFound", innerException: null), true };
            yield return new object[] { new RequestFailedException(403, "Another kind of error.", "AnotherKindOfError", innerException: null), false };
            yield return new object[] { new RequestFailedException("Undefined error"), false };
        }
    }

    [Theory]
    [MemberData(nameof(GetExceptionToResultMapping))]
    public void GivenRequestFailedException_WhenIsCMKErrorIsCalled_ThenReturnExpectedResult(RequestFailedException exception, bool expectedResult)
    {
        bool result = exception.IsCMKError();
        Assert.Equal(expectedResult, result);
    }
}
