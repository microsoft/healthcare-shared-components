﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Core.Features.Security.Authorization
{
    public class RoleBasedAuthorizationService<TEnum, TContext> : IAuthorizationService<TEnum>
        where TEnum : Enum
        where TContext : IRequestContext
    {
        private readonly GenericRequestContextAccessor<TContext> _dicomRequestContextAccessor;
        private readonly string _rolesClaimName;
        private readonly Dictionary<string, Role<TEnum>> _roles;

        public RoleBasedAuthorizationService(AuthorizationConfiguration<TEnum> authorizationConfiguration, GenericRequestContextAccessor<TContext> dicomRequestContextAccessor)
        {
            EnsureArg.IsNotNull(authorizationConfiguration, nameof(authorizationConfiguration));
            _dicomRequestContextAccessor = EnsureArg.IsNotNull(dicomRequestContextAccessor, nameof(dicomRequestContextAccessor));

            _rolesClaimName = authorizationConfiguration.RolesClaim;
            _roles = authorizationConfiguration.Roles.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        public ValueTask<TEnum> CheckAccess(TEnum dataActions, CancellationToken cancellationToken)
        {
            ClaimsPrincipal principal = _dicomRequestContextAccessor.RequestContext.Principal;

            ulong permittedDataActions = 0;
            ulong dataActionsUlong = Convert.ToUInt64(dataActions, NumberFormatInfo.InvariantInfo);
            foreach (Claim claim in principal.FindAll(_rolesClaimName))
            {
                if (_roles.TryGetValue(claim.Value, out Role<TEnum> role))
                {
                    permittedDataActions |= role.AllowedDataActionsUlong;
                    if (permittedDataActions == dataActionsUlong)
                    {
                        break;
                    }
                }
            }

            return new ValueTask<TEnum>((TEnum)Enum.ToObject(typeof(TEnum), dataActionsUlong & permittedDataActions));
        }
    }
}
