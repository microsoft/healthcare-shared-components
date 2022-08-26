// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Client.Authentication.Exceptions;

public class FailToRetrieveTokenException : Exception
{
    public FailToRetrieveTokenException()
    {
    }

    public FailToRetrieveTokenException(string message)
        : base(message)
    {
    }

    public FailToRetrieveTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
