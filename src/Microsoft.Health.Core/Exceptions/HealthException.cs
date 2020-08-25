// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Core.Exceptions
{
    public class HealthException : Exception
    {
        public HealthException(string message)
            : base(message)
        {
        }

        public HealthException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
