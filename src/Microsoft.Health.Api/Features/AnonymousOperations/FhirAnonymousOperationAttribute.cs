// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Health.Api.Features.AnonymousOperation;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FhirAnonymousOperationAttribute : AllowAnonymousAttribute
{
    public FhirAnonymousOperationAttribute(string fhirOperation)
    {
        EnsureArg.IsNotNull(fhirOperation, nameof(fhirOperation));
        FhirOperation = fhirOperation;
    }

    public string FhirOperation { get; }
}
