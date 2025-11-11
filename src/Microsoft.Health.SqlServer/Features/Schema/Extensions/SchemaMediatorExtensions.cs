// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Medino;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;

namespace Microsoft.Health.SqlServer.Features.Schema.Extensions;

public static class SchemaMediatorExtensions
{
    public static Task<GetCompatibilityVersionResponse> GetCompatibleVersionAsync(this IMediator mediator, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));

        var request = new GetCompatibilityVersionRequest();

        return mediator.SendAsync(request, cancellationToken);
    }

    public static Task<GetCurrentVersionResponse> GetCurrentVersionAsync(this IMediator mediator, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));

        var request = new GetCurrentVersionRequest();

        return mediator.SendAsync(request, cancellationToken);
    }

    public static Task NotifySchemaUpgradedAsync(this IMediator mediator, int version, bool isFullSchemaSnapshotUpgrade, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(mediator, nameof(mediator));

        return mediator.PublishAsync(new SchemaUpgradedNotification(version, isFullSchemaSnapshotUpgrade), cancellationToken);
    }
}
