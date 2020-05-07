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
        public async Task GivenAScript_WhenDiffSegmentIsPresent_ThenReturnsDiffAsync()
        {
            var diffSegment = ".diff";
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(2, diffSegment));
        }

        [Fact]
        public async Task GivenAScript_WhenDiffSegmentIsEmpty_ThenReturnsCompleteScriptAsync()
        {
            var diffSegment = string.Empty;
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(1, diffSegment));
        }

        [Fact]
        public async Task GivenAScriptIsNotPresent_WhenVersionIsNotKnown_ThenReturnsFileNotFoundException()
        {
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _scriptProvider.GetMigrationScriptAsBytesAsync(3, string.Empty));
            Assert.Equal("The provided version is unknown.", ex.Message);
        }
    }
}
