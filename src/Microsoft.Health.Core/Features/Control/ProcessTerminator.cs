// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Core.Features.Control;

public class ProcessTerminator : IProcessTerminator
{
    private readonly IHostApplicationLifetime _lifetime;

    public ProcessTerminator(IHostApplicationLifetime applicationLifetime)
    {
        _lifetime = EnsureArg.IsNotNull(applicationLifetime, nameof(applicationLifetime));
    }

    public void Terminate(CancellationToken cancellationToken)
    {
        try
        {
            OnBeforeTerminate(cancellationToken);
        }
        catch (TaskCanceledException)
        {
        }

        Environment.ExitCode = 0;
        _lifetime.StopApplication();
    }

    protected virtual Task OnBeforeTerminate(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
