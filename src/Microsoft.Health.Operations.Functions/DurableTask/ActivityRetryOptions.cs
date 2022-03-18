// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <inheritdoc cref="RetryOptions" />
public class ActivityRetryOptions
{
    /// <inheritdoc cref="RetryOptions.FirstRetryInterval" />
    [Required]
    [Range(typeof(TimeSpan), "00:00:00.0010000", "10675199.02:48:05.4775807", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan FirstRetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <inheritdoc cref="RetryOptions.MaxRetryInterval" />
    public TimeSpan MaxRetryInterval { get; set; } = TimeSpan.FromDays(6); // Default from Durable Functions

    /// <inheritdoc cref="RetryOptions.BackoffCoefficient" />
    [Range(0d, double.MaxValue)]
    public double BackoffCoefficient { get; set; } = 1;

    /// <inheritdoc cref="RetryOptions.RetryTimeout" />
    [Range(typeof(TimeSpan), "-00:00:00.0010000", "10675199.02:48:05.4775807", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan RetryTimeout { get; set; } = TimeSpan.MaxValue;

    /// <inheritdoc cref="RetryOptions.MaxNumberOfAttempts" />
    [Range(1, int.MaxValue)]
    public int MaxNumberOfAttempts { get; set; } = 1;

    /// <inheritdoc cref="RetryOptions.Handle" />
    public Func<Exception, bool> Handle { get; set; } = e => true;

    public RetryOptions ToRetryOptions()
        => new RetryOptions(FirstRetryInterval, MaxNumberOfAttempts)
        {
            BackoffCoefficient = BackoffCoefficient,
            Handle = Handle,
            MaxRetryInterval = MaxRetryInterval,
            RetryTimeout = RetryTimeout,
        };

    public static implicit operator RetryOptions(ActivityRetryOptions o)
        => EnsureArg.IsNotNull(o, nameof(o)).ToRetryOptions();
}
