// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health;

internal class SQLInaccessibleStateTestProvider : ICustomerKeyTestProvider
{
    private const string InaccessibleMessage = "The SQL DB state is Inaccessible";

    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
    private readonly ILogger<SQLInaccessibleStateTestProvider> _logger;

    public SQLInaccessibleStateTestProvider(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ILogger<SQLInaccessibleStateTestProvider> logger)
    {
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _logger = EnsureArg.IsNotNull(_logger, nameof(logger));
    }

    public int Priority => 2;

    public HealthStatusReason FailureReason => HealthStatusReason.DataStoreStateDegraded;

    public async Task<CustomerKeyHealth> AssertHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
            return new CustomerKeyHealth();
        }
        // Error: Can not connect to the database in its current state. This error can be for various DB states (recovering, inacessible) but we assume that our DB will only hit this for Inaccessible state
        catch (SqlException ex) when (ex.ErrorCode == 40925)
        {
            _logger.LogInformation(ex, InaccessibleMessage);

            return new CustomerKeyHealth
            {
                IsHealthy = false,
                Reason = FailureReason,
                Exception = ex,
            };
        }
    }
}
