﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Abstractions.Features.Transactions;

namespace Microsoft.Health.SqlServer.Features.Storage;

public class SqlTransactionHandler : ITransactionHandler
{
    public SqlTransactionScope SqlTransactionScope { get; private set; }

    public ITransactionScope BeginTransaction()
    {
        Debug.Assert(SqlTransactionScope == null, "The existing SQL transaction scope should be completed before starting a new transaction.");

        if (SqlTransactionScope != null)
        {
            throw new TransactionFailedException();
        }

        SqlTransactionScope = new SqlTransactionScope(this);

        return SqlTransactionScope;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            SqlTransactionScope?.Dispose();

            SqlTransactionScope = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
