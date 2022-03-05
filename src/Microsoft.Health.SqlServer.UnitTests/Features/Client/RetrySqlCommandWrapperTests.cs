// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Client;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client
{
    public class RetrySqlCommandWrapperTests
    {
        [Fact]
        public void GivenASqlCommandWrapper_ItSRetryPolicy_IsExponentialRetry()
        {
            var retryOption = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 3,
            });
            var sqlCommandWrapper = new RetrySqlCommandWrapper(
                                        new SqlCommandWrapper(new Data.SqlClient.SqlCommand()),
                                        retryOption);

            Assert.True(sqlCommandWrapper.RetryLogicProvider == retryOption);
        }
    }
}
