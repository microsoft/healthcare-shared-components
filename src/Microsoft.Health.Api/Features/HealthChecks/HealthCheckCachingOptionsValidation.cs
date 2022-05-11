// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using EnsureThat;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Api.Features.HealthChecks;

/// <summary>
/// Represents the validation of <see cref="HealthCheckCachingOptions"/> instances.
/// </summary>
public sealed class HealthCheckCachingOptionsValidation : IValidateOptions<HealthCheckCachingOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string name, HealthCheckCachingOptions options)
    {
        // Note: We don't currently differentiate between any potential named options, so name parameter is ignored
        EnsureArg.IsNotNull(options, nameof(options));
        var failures = new List<string>();

        // Expiry
        if (options.Expiry < TimeSpan.Zero)
        {
            failures.Add(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.PropertyCannotBeLessThanValue,
                    nameof(HealthCheckCachingOptions.Expiry),
                    options.Expiry,
                    TimeSpan.Zero));
        }

        // RefreshOffset
        if (options.RefreshOffset < TimeSpan.Zero)
        {
            failures.Add(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.PropertyCannotBeLessThanValue,
                    nameof(HealthCheckCachingOptions.RefreshOffset),
                    options.RefreshOffset,
                    TimeSpan.Zero));
        }

        // MaxRefreshThreads
        if (options.MaxRefreshThreads < 1)
        {
            failures.Add(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.PropertyCannotBeLessThanValue,
                    nameof(HealthCheckCachingOptions.MaxRefreshThreads),
                    options.MaxRefreshThreads,
                    1));
        }

        // Expiry + RefreshOffset
        if (options.Expiry >= TimeSpan.Zero && options.RefreshOffset > options.Expiry)
        {
            failures.Add(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.PropertyCannotBeLargerThanAnother,
                    nameof(HealthCheckCachingOptions.RefreshOffset),
                    options.RefreshOffset,
                    nameof(HealthCheckCachingOptions.Expiry),
                    options.Expiry));
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
