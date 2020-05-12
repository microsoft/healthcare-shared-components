﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.SqlServer.Features.Exceptions
{
    public class SqlRecordNotFoundException : MicrosoftHealthException
    {
        public SqlRecordNotFoundException(string message)
            : base(message)
        {
            EnsureArg.IsNotNullOrWhiteSpace(message, nameof(message));
        }
    }
}
