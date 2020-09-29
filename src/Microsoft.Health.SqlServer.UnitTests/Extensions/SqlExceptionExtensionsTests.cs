// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Extensions;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Extensions
{
    public class SqlExceptionExtensionsTests
    {
        [Theory]
        [InlineData(10928)]
        [InlineData(10929)]
        [InlineData(10053)]
        [InlineData(10054)]
        [InlineData(10060)]
        [InlineData(18401)]
        [InlineData(40197)]
        [InlineData(40540)]
        [InlineData(40613)]
        [InlineData(40143)]
        [InlineData(233)]
        [InlineData(64)]
        public void GivenATransientException_WhenCheckedIfExceptionIsTransient_ThenTrueShouldBeReturned(int number)
        {
            SqlException sqlException = SqlExceptionFactory.Create(number);

            Assert.True(sqlException.IsTransient());
        }

        [Fact]
        public void GivenANonTransientException_WhenCheckedIfExceptionIsTransient_ThenFalseShouldBeReturned()
        {
            SqlException sqlException = SqlExceptionFactory.Create(10001);

            Assert.False(sqlException.IsTransient());
        }
    }
}
