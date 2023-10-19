// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health;

internal class SQLInaccessibleStateTestProvider : IDataStoreStateTestProvider
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

    public async Task AssertHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
        }
        // Error: Can not connect to the database in its current state. This error can be for various DB states (recovering, inacessible) but we assume that our DB will only hit this for Inaccessible state
        catch (SqlException ex) when (ex.ErrorCode == 40925)
        {
            _logger.LogInformation(ex, InaccessibleMessage);

            throw new DataStoreStateInaccessibleException(InaccessibleMessage, ex);
        }
    }
}
