// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client;

/// <summary>
/// Manages SQL connection pool clearing in response to transport-level errors.
/// When a transport error (e.g., TCP RST from VFP reprogramming) is detected,
/// all pooled connections are likely dead and must be discarded.
/// A cooldown prevents excessive clearing from concurrent error storms.
/// </summary>
public class SqlConnectionPoolManager
{
    private readonly ISqlConnectionPoolResetter _poolResetter;
    private readonly ILogger<SqlConnectionPoolManager> _logger;
    private readonly TimeSpan _cooldown;

    private long _lastResetTicks;

    /// <summary>
    /// Default cooldown of 30 seconds. VFP reprogramming completes in 1-3s, so this allows
    /// full recovery while preventing thundering-herd clears from concurrent error storms.
    /// </summary>
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(30);

    public SqlConnectionPoolManager(
        ISqlConnectionPoolResetter poolResetter,
        ILogger<SqlConnectionPoolManager> logger,
        TimeSpan? cooldown = null)
    {
        _poolResetter = poolResetter ?? throw new ArgumentNullException(nameof(poolResetter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cooldown = cooldown ?? DefaultCooldown;
    }

    /// <summary>
    /// Call when a <see cref="SqlException"/> is caught. If the exception is a transport-level
    /// error, clears all connection pools (subject to cooldown).
    /// </summary>
    /// <param name="exception">The SQL exception that was caught.</param>
    /// <returns>True if the pool was cleared; false if the error was not a transport error or cooldown is active.</returns>
    public bool HandleError(SqlException exception)
    {
        if (exception == null || !IsTransportError(exception))
        {
            return false;
        }

        return TryClearPools(exception);
    }

    private bool TryClearPools(SqlException exception)
    {
        long now = DateTime.UtcNow.Ticks;
        long last = Interlocked.Read(ref _lastResetTicks);
        long cooldownTicks = _cooldown.Ticks;

        if (now - last <= cooldownTicks)
        {
            _logger.LogDebug(
                "SQL connection pool clear skipped (cooldown active). Last clear was {ElapsedMs}ms ago.",
                TimeSpan.FromTicks(now - last).TotalMilliseconds);
            return false;
        }

        if (Interlocked.CompareExchange(ref _lastResetTicks, now, last) != last)
        {
            // Another thread won the race
            return false;
        }

        _logger.LogWarning(
            exception,
            "Transport-level SQL error detected (error code: {ErrorCode}). Clearing all SQL connection pools to discard stale connections.",
            exception.Number);

        _poolResetter.ClearAllPools();
        return true;
    }

    /// <summary>
    /// Determines whether the given <see cref="SqlException"/> represents a transport-level error
    /// indicating that the underlying TCP connection has been reset or severed.
    /// </summary>
    internal static bool IsTransportError(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (error.Number is SqlErrorCodes.TransportLevelError
                or SqlErrorCodes.SemaphoreTimeout
                or SqlErrorCodes.ConnectionForciblyClosedByRemoteHost)
            {
                return true;
            }
        }

        return false;
    }
}
