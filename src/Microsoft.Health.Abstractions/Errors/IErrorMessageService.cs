// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Abstractions.Errors
{
    public interface IErrorMessageService
    {
        /// <summary>
        /// Writes a collection of ErrorMessages to a destination
        /// </summary>
        /// <param name="errorMessages">A collection of error message objects.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><see cref="Task"/>Returns the successfully stored error messages.</returns>
        Task<IEnumerable<ErrorMessage>> ReportError(IEnumerable<ErrorMessage> errorMessages, CancellationToken cancellationToken);
    }
}
