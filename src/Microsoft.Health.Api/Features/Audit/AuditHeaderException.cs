﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Exceptions;

namespace Microsoft.Health.Api.Features.Audit
{
    public class AuditHeaderException : HealthException
    {
        public AuditHeaderException(string message)
            : base(message)
        {
        }
    }
}
