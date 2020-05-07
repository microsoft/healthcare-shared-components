// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema
{
    public class ScriptProviderTests
    {
        private readonly ScriptProvider<TestSchemaVersionEnum> _scriptProvider;

        public ScriptProviderTests()
        {
            _scriptProvider = new ScriptProvider<TestSchemaVersionEnum>();
        }

        [Fact]
        public async Task GivenAScript_WhenDiffIsFalse_ThenReturnsCompleteScriptScriptAsync()
        {
            bool isDiff = false;
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(1, isDiff));
        }

        [Fact]
        public async Task GivenAScript_WhenDiffIsTrue_ThenReturnsDiffScriptScriptAsync()
        {
            bool isDiff = true;
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(2, isDiff));
        }

        [Fact]
        public async Task GivenAScriptIsNotPresent_WhenVersionIsNotKnown_ThenReturnsFileNotFoundException()
        {
            bool isDiff = true;
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _scriptProvider.GetMigrationScriptAsBytesAsync(3, isDiff));
            Assert.Equal("The provided version is unknown.", ex.Message);
        }
    }
}
