﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Features
{
    public class CurrentVersionSchemaHandlerTests
    {
        private readonly ISchemaDataStore _schemaDataStore;
        private readonly IMediator _mediator;
        private readonly CancellationToken _cancellationToken;

        public CurrentVersionSchemaHandlerTests()
        {
            _schemaDataStore = Substitute.For<ISchemaDataStore>();
            var collection = new ServiceCollection();
            collection.Add(sp => new CurrentVersionSchemaHandler(_schemaDataStore)).Singleton().AsSelf().AsImplementedInterfaces();

            ServiceProvider provider = collection.BuildServiceProvider();
            _mediator = new Mediator(type => provider.GetService(type));
            _cancellationToken = new CancellationTokenSource().Token;
        }

        [Fact]
        public async Task GivenACurrentMediator_WhenCurrentRequest_ThenReturnsCurrentVersionInformation()
        {
            var mockCurrentVersions = new List<CurrentVersionInformation>()
            {
                new CurrentVersionInformation(1, "completed", new List<string>() { "server1", "server2" }),
                new CurrentVersionInformation(1, "completed", new List<string>()),
            };

            _schemaDataStore.GetCurrentVersionAsync(Arg.Any<CancellationToken>())
                    .Returns(mockCurrentVersions);
            GetCurrentVersionResponse response = await _mediator.GetCurrentVersionAsync(_cancellationToken);

            Assert.Equal(mockCurrentVersions.Count, response.CurrentVersions.Count);
        }

        [Fact]
        public async Task GivenACurrentMediator_WhenCurrentRequestAndEmptySchemaVersionTable_ThenReturnsEmptyArray()
        {
            var mockCurrentVersions = new List<CurrentVersionInformation>();

            _schemaDataStore.GetCurrentVersionAsync(Arg.Any<CancellationToken>())
                    .Returns(mockCurrentVersions);

            GetCurrentVersionResponse response = await _mediator.GetCurrentVersionAsync(_cancellationToken);

            Assert.Equal(0, response.CurrentVersions.Count);
        }
    }
}
