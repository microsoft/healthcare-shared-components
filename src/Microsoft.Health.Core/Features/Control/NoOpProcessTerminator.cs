// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Core.Features.Control
{
    public class NoOpProcessTerminator : IProcessTerminator
    {
        private readonly ILogger<NoOpProcessTerminator> _logger;

        public NoOpProcessTerminator(ILogger<NoOpProcessTerminator> logger)
        {
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public void Terminate(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Process termination was requested from the NoOpProcessTerminator.");
        }
    }
}
