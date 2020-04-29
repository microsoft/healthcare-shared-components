﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client
{
    public class SqlConnectionWrapperFactory
    {
        private readonly SqlServerDataStoreConfiguration _configuration;
        private readonly SqlTransactionHandler _sqlTransactionHandler;
        private readonly SqlCommandWrapperFactory _sqlCommandFactory;

        public SqlConnectionWrapperFactory(
            SqlServerDataStoreConfiguration configuration,
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandFactory)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandFactory, nameof(sqlCommandFactory));

            _configuration = configuration;
            _sqlTransactionHandler = sqlTransactionHandler;
            _sqlCommandFactory = sqlCommandFactory;
        }

        public SqlConnectionWrapper ObtainSqlConnectionWrapper(bool enlistInTransaction = false)
        {
            return new SqlConnectionWrapper(_configuration, _sqlTransactionHandler, _sqlCommandFactory, enlistInTransaction);
        }
    }
}