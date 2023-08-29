// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Encryption.Customer.Health;

public class CustomerKeyInaccessibleException : Exception
{
    public CustomerKeyInaccessibleException(string message) : base(message)
    {
    }

    public CustomerKeyInaccessibleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
