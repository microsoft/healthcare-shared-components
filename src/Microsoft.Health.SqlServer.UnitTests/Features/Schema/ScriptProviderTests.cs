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
        public async Task GivenASnapshotScript_WhenGetDiffScriptAsBytesAsync_ThenReturnsDiffScriptAsync()
        {
            Assert.NotNull(await _scriptProvider.GetDiffScriptAsBytesAsync(2));
        }

        [Fact]
        public async Task GivenADiffScript_WhenGetSnapshotScriptAsBytesAsync_ThenReturnsSnapshotScriptAsync()
        {
            Assert.NotNull(await _scriptProvider.GetSnapshotScriptAsBytesAsync(1));
        }

        [Fact]
        public async Task GivenASnapshotScriptNotPresent_WhenGetSnapshotScriptAsBytesAsync_ThenReturnsFileNotFoundException()
        {
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _scriptProvider.GetSnapshotScriptAsBytesAsync(2));
            Assert.Equal("The provided version is unknown.", ex.Message);
        }

        [Fact]
        public async Task GivenADiffScriptNotPresent_WhenGetDiffScriptAsBytesAsync_ThenReturnsFileNotFoundException()
        {
            FileNotFoundException ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _scriptProvider.GetDiffScriptAsBytesAsync(1));
            Assert.Equal("The provided version is unknown.", ex.Message);
        }
    }
}
