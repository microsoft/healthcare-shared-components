// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    public class RetrySqlAction
    {
        private readonly IAsyncPolicy policy;

        public RetrySqlAction(ISqlServerTransientFaultRetryPolicyFactory sqlServerTransientFault)
        {
            EnsureArg.IsNotNull(sqlServerTransientFault, nameof(sqlServerTransientFault));
            policy = sqlServerTransientFault.Create();
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            await policy.ExecuteAsync(action);
        }
    }
}
