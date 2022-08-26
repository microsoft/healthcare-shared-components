// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.SqlServer.Features.Exceptions;

public class SqlOperationFailedException : MicrosoftHealthException
{
    public SqlOperationFailedException()
    {
    }

    public SqlOperationFailedException(string message)
        : base(message)
    {
        EnsureArg.IsNotNullOrWhiteSpace(message, nameof(message));
    }

    public SqlOperationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
