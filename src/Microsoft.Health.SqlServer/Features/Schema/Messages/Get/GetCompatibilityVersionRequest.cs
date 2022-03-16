// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

#pragma warning disable CA1716 // Identifiers should not match keywords

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Get;

public class GetCompatibilityVersionRequest : IRequest<GetCompatibilityVersionResponse>
{
}
