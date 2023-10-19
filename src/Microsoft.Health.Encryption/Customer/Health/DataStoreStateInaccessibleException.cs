// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Encryption.Customer.Health;

public class DataStoreStateInaccessibleException : Exception
{
    public DataStoreStateInaccessibleException() { }

    public DataStoreStateInaccessibleException(string message) : base(message)
    {
    }

    public DataStoreStateInaccessibleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
