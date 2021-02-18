// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Core.Features.Security.Authorization
{
    /// <summary>
    /// Service used for checking if given set of dataActions are present in a given request contexts principal.
    /// </summary>
    /// <typeparam name="TDataActions">Type representing the dataActions for the service</typeparam>
    /// <typeparam name="TRequestContext">Type representing the IRequestContext implementation for the service</typeparam>
    public class RoleBasedAuthorizationService<TDataActions, TRequestContext> : IAuthorizationService<TDataActions>
        where TDataActions : Enum
        where TRequestContext : IRequestContext
    {
        private readonly RequestContextAccessor<TRequestContext> _requestContextAccessor;
        private readonly string _rolesClaimName;
        private readonly Dictionary<string, Role<TDataActions>> _roles;

        private static readonly Func<TDataActions, ulong> ConvertToULong = CreateConvertToULongFunc();
        private static readonly Func<ulong, TDataActions> ConvertToTDataAction = CreateConvertToTDataActionFunc();

        public RoleBasedAuthorizationService(AuthorizationConfiguration<TDataActions> authorizationConfiguration, RequestContextAccessor<TRequestContext> requestContextAccessor)
        {
            EnsureArg.IsNotNull(authorizationConfiguration, nameof(authorizationConfiguration));
            _requestContextAccessor = EnsureArg.IsNotNull(requestContextAccessor, nameof(requestContextAccessor));

            _rolesClaimName = authorizationConfiguration.RolesClaim;
            _roles = authorizationConfiguration.Roles.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static Func<TDataActions, ulong> CreateConvertToULongFunc()
        {
            var parameterExpression = Expression.Parameter(typeof(TDataActions));
            return Expression.Lambda<Func<TDataActions, ulong>>(Expression.Convert(parameterExpression, typeof(ulong)), parameterExpression).Compile();
        }

        private static Func<ulong, TDataActions> CreateConvertToTDataActionFunc()
        {
            var parameterExpression = Expression.Parameter(typeof(ulong));
            return Expression.Lambda<Func<ulong, TDataActions>>(Expression.Convert(parameterExpression, typeof(TDataActions)), parameterExpression).Compile();
        }

        public ValueTask<TDataActions> CheckAccess(TDataActions dataActions, CancellationToken cancellationToken)
        {
            ClaimsPrincipal principal = _requestContextAccessor.RequestContext.Principal;

            ulong permittedDataActions = 0;
            ulong dataActionsUlong = ConvertToULong(dataActions);
            foreach (Claim claim in principal.FindAll(_rolesClaimName))
            {
                if (_roles.TryGetValue(claim.Value, out Role<TDataActions> role))
                {
                    permittedDataActions |= role.AllowedDataActionsUlong;
                    if (permittedDataActions == dataActionsUlong)
                    {
                        break;
                    }
                }
            }

            return new ValueTask<TDataActions>(ConvertToTDataAction(dataActionsUlong & permittedDataActions));
        }
    }
}
