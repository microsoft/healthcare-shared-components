﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Api.Extensions;

namespace Microsoft.Health.Api.Features.Audit
{
    /// <summary>
    /// Provides the ability to lookup audit event type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normally, the MVC middleware handles the routing which maps the controller name and action name to a method within the controller.
    /// </para>
    /// <para>
    /// The <see cref="AuditEventTypeAttribute"/> that contains the audit event type information is defined on the method so we need the method
    /// in order to be able to retrieve the attribute. However, since authentication middleware runs before the MVC middleware, if the authentication
    /// rejects the call for whatever reason, we will not be able to retrieve the attribute and therefore, will not be able to get the corresponding audit event type.
    /// </para>
    /// <para>
    /// This class builds the mapping ahead of time so that we can lookup the audit event type any time during the pipeline given the controller name and action name.
    /// </para>
    /// </remarks>
    public class AuditEventTypeMapping : IAuditEventTypeMapping, IHostedService
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        private IReadOnlyDictionary<(string ControllerName, string ActionName), Attribute> _attributeDictionary;

        public AuditEventTypeMapping(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            EnsureArg.IsNotNull(actionDescriptorCollectionProvider, nameof(actionDescriptorCollectionProvider));

            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        /// <inheritdoc />
        public string GetAuditEventType(string controllerName, string actionName)
        {
            if (!_attributeDictionary.TryGetValue((controllerName, actionName), out Attribute attribute))
            {
                throw new MissingAuditEventTypeMappingException(controllerName, actionName);
            }

            if (attribute is AuditEventTypeAttribute auditEventTypeAttribute)
            {
                return auditEventTypeAttribute.AuditEventType;
            }

            return null;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: check that different actions are not using the same audit type
            _attributeDictionary = _actionDescriptorCollectionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(ad =>
                {
                    Attribute attribute = ad.MethodInfo?.GetCustomAttributes<AllowAnonymousAttribute>().FirstOrDefault() ??
                                          (Attribute)ad.MethodInfo?.GetCustomAttributes<AuditEventTypeAttribute>().FirstOrDefault();

                    return (ad.ControllerName, ad.ActionName, Attribute: attribute);
                })
                .Where(item => item.Attribute != null)
                .Distinct()
                .GroupBy(
                    x => (x.ControllerName, x.ActionName),
                    x => x.Attribute,
                    (key, values) =>
                    {
                        List<Attribute> attributes = values.ToList();
                        if (attributes.Count > 1)
                        {
                            throw new DuplicateActionForAuditEventException(key.ControllerName, key.ActionName);
                        }

                        return (key, attributes[0]);
                    })
                .ToDictionary(
                    item => item.key,
                    item => item.Item2);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
