// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when provided definition is invalid.
    /// </summary>
    public class InvalidDefinitionException : MicrosoftHealthException
    {
        public InvalidDefinitionException(string message)
            : base(message)
        {
        }
    }
}
