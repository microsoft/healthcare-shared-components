// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;

namespace Microsoft.Health.Core.Features.Control
{
    public interface IProcessTerminator
    {
        /// <summary>
        /// Terminates the current process.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        public void Terminate(CancellationToken cancellationToken);
    }
}
