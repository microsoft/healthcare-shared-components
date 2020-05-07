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
        private const string DiffSegment = ".diff";

        public ScriptProviderTests()
        {
            _scriptProvider = new ScriptProvider<TestSchemaVersionEnum>();
        }

        [Fact]
        public async Task GivenAScript_WhenDiffSegmentIsPresent_ThenReturnsDiffAsync()
        {
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(2, DiffSegment));
        }

        [Fact]
        public async Task GivenAScript_WhenDiffSegmentIsEmpty_ThenReturnsCompleteScriptAsync()
        {
            Assert.NotNull(await _scriptProvider.GetMigrationScriptAsBytesAsync(1, string.Empty));
        }

        [InlineData("")]
        [InlineData(DiffSegment)]
        [Theory]
        public async Task GivenAScriptIsNotPresent_WhenDiffIsNotPresent_ThenReturnsFileNotFoundException(string diffSegment)
        {
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _scriptProvider.GetMigrationScriptAsBytesAsync(3, diffSegment));
            Assert.Equal("The provided version is unknown.", ex.Message);
        }
    }
}
