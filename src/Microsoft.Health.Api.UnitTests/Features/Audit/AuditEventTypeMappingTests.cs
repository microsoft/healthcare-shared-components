// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Api.Features.Audit;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.Audit
{
    public class AuditEventTypeMappingTests : IAsyncLifetime
    {
        private const string ControllerName = nameof(MockController);
        private const string AnonymousMethodName = nameof(MockController.Anonymous);
        private const string AudittedMethodName = nameof(MockController.Auditted);
        private const string MultipleRoutesMethodName = nameof(MockController.MultipleRoutes);
        private const string SameNameMethodName = nameof(MockController.SameName);
        private const string NoAttributeMethodName = nameof(MockController.NoAttribute);
        private const string AuditEventType = "audit";
        private const string AnotherAuditEventType = "anotherAudit";

        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider = Substitute.For<IActionDescriptorCollectionProvider>();
        private readonly AuditEventTypeMapping _auditEventTypeMapping;

        public AuditEventTypeMappingTests()
        {
            Type mockControllerType = typeof(MockController);

            var actionDescriptors = new List<ActionDescriptor>()
            {
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = AnonymousMethodName,
                    MethodInfo = mockControllerType.GetMethod(AnonymousMethodName),
                },
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = AudittedMethodName,
                    MethodInfo = mockControllerType.GetMethod(AudittedMethodName),
                },
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = NoAttributeMethodName,
                    MethodInfo = mockControllerType.GetMethod(NoAttributeMethodName),
                },
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = MultipleRoutesMethodName,
                    MethodInfo = mockControllerType.GetMethod(MultipleRoutesMethodName),
                },
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = MultipleRoutesMethodName,
                    MethodInfo = mockControllerType.GetMethod(MultipleRoutesMethodName),
                },
                new PageActionDescriptor()
                {
                },
            };

            var actionDescriptorCollection = new ActionDescriptorCollection(actionDescriptors, 1);

            _actionDescriptorCollectionProvider.ActionDescriptors.Returns(actionDescriptorCollection);

            _auditEventTypeMapping = new AuditEventTypeMapping(_actionDescriptorCollectionProvider);
        }

        public async Task InitializeAsync()
        {
            await ((IHostedService)_auditEventTypeMapping).StartAsync(CancellationToken.None);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData(ControllerName, AnonymousMethodName, null)]
        [InlineData(ControllerName, AudittedMethodName, AuditEventType)]
        [InlineData(ControllerName, MultipleRoutesMethodName, AuditEventType)]
        public void GivenControllerNameAndActionName_WhenGetAuditEventTypeIsCalled_ThenAuditEventTypeShouldBeReturned(string controllerName, string actionName, string expectedAuditEventType)
        {
            string actualAuditEventType = _auditEventTypeMapping.GetAuditEventType(controllerName, actionName);

            Assert.Equal(expectedAuditEventType, actualAuditEventType);
        }

        [Fact]
        public void GivenUnknownControllerNameAndActionName_WhenGetAuditEventTypeIsCalled_ThenAuditExceptionShouldBeThrown()
        {
            Assert.Throws<MissingAuditEventTypeMappingException>(() => _auditEventTypeMapping.GetAuditEventType("test", "action"));
        }

        [Fact]
        public void GivenTwoMethodsWithTheSameNameAndDifferentAuditEvents_WhenMappingIsCreated_ThenDuplicateActionForAuditEventExceptionShouldBeThrown()
        {
            Type mockControllerType = typeof(MockController);

            var actionDescriptors = new List<ActionDescriptor>()
            {
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = SameNameMethodName,
                    MethodInfo = mockControllerType.GetMethod(SameNameMethodName, new Type[] { typeof(int) }),
                },
                new ControllerActionDescriptor()
                {
                    ControllerName = ControllerName,
                    ActionName = SameNameMethodName,
                    MethodInfo = mockControllerType.GetMethod(SameNameMethodName, new Type[] { typeof(string) }),
                },
            };

            var actionDescriptorCollection = new ActionDescriptorCollection(actionDescriptors, 1);
            _actionDescriptorCollectionProvider.ActionDescriptors.Returns(actionDescriptorCollection);

            var eventTypeMapping = new AuditEventTypeMapping(_actionDescriptorCollectionProvider);

            Assert.ThrowsAsync<DuplicateActionForAuditEventException>(() => ((IHostedService)eventTypeMapping).StartAsync(CancellationToken.None));
        }

        private class MockController : Controller
        {
            [AllowAnonymous]
            public IActionResult Anonymous() => new OkResult();

            [AuditEventType(AuditEventType)]
            public IActionResult Auditted() => new OkResult();

            [Route("some/route")]
            [Route("another/route")]
            [AuditEventType(AuditEventType)]
            public IActionResult MultipleRoutes() => new OkResult();

            [AuditEventType(AuditEventType)]
            public IActionResult SameName(int x) => new OkResult();

            [AuditEventType(AnotherAuditEventType)]
            public IActionResult SameName(string y) => new OkResult();

            public IActionResult NoAttribute() => new OkResult();
        }
    }
}
