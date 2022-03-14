﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Get;

public class GetCompatibilityVersionRequest : IRequest<GetCompatibilityVersionResponse>
{
}
