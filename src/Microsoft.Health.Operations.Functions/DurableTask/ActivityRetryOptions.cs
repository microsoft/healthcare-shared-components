// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.DurableTask;

// The purpose of this class is to provide a class that can be bound to
// a configuration through the options APIs. By default, RetryOptions
// cannot be used, as it does not have a parameterless constructor

/// <inheritdoc cref="RetryOptions" />
public class ActivityRetryOptions : RetryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRetryOptions"/> class
    /// whose <see cref="RetryOptions.FirstRetryInterval"/> is defaulted to one second
    /// and only attempts one time.
    /// </summary>
    public ActivityRetryOptions()
        : base(TimeSpan.FromSeconds(1), 1)
    { }
}
