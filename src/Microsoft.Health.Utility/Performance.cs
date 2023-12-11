// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using EnsureThat;

namespace Microsoft.Health.Utility;

public static class Performance
{
    /// <summary>
    /// Track the duration of the execution time in the life cycle of the ITimed.
    /// </summary>
    /// <param name="handler">The callback to handle the duration time.</param>
    /// <returns></returns>
    public static ITimed TrackDuration(Action<double> handler)
    {
        EnsureArg.IsNotNull(handler, nameof(handler));

        return new TimedHandler
        {
            Stopwatch = Stopwatch.StartNew(),
            Handler = handler,
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "The struct is Private.")]
    private struct TimedHandler : ITimed
    {
        public Stopwatch Stopwatch { get; set; }

        public Action<double> Handler { get; set; }

        public TimeSpan Elapsed => Stopwatch.Elapsed;

        public ITimed Record()
        {
            Handler(Elapsed.TotalMilliseconds);
            return this;
        }

        public void Dispose()
        {
            Stopwatch.Stop();
            Record();
        }
    }
}
