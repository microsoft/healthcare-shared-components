// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Models;

namespace Microsoft.Health.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when provided definition is invalid.
    /// </summary>
    public class InvalidDefinitionException : HealthException
    {
        public InvalidDefinitionException(string message, OperationOutcomeIssue[] issues = null)
            : base(message, issues)
        {
        }
    }
}
