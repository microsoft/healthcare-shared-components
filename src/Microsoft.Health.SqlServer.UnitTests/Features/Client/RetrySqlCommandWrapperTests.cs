// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client
{
    public class RetrySqlCommandWrapperTests
    {
        [Fact]
        public void GivenASqlCommandWrapper_ItSRetryPolicy_IsExponentialRetry()
        {
        }

        [Fact]
        public void GivenASqlConnectionWrapper_ItSRetryPolicy_IsExponentialRetry()
        {
        }
    }
}
