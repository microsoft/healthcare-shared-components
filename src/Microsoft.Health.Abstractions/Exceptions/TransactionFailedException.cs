// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Abstractions.Exceptions;

public class TransactionFailedException : MicrosoftHealthException
{
    public TransactionFailedException()
        : base(Resources.TransactionProcessingException)
    {
    }

    public TransactionFailedException(string message)
        : base(message)
    {
    }

    public TransactionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
