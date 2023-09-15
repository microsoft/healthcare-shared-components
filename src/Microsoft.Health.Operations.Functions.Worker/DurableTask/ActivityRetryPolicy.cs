// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.DurableTask;

namespace Microsoft.Health.Operations.Functions.Worker.DurableTask;

// The purpose of this class is to provide a class that can be bound to
// a configuration through the options APIs. By default, RetryPolicy
// cannot be used, as it does not have a parameterless constructor

/// <inheritdoc cref="RetryPolicy" />
public class ActivityRetryPolicy : RetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRetryPolicy"/> class
    /// whose <see cref="RetryPolicy.FirstRetryInterval"/> is defaulted to one second
    /// and only attempts one time.
    /// </summary>
    public ActivityRetryPolicy()
        : base(1, TimeSpan.FromSeconds(1))
    { }
}
