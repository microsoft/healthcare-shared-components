// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using SchemaManager.Core;
using SchemaManager.Core.Model;
using SchemaManager.Validators;

namespace SchemaManager.Commands;

public class ApplyCommand : Command
{
    private readonly ISchemaManager _schemaManager;
    private readonly SchemaClient _schemaClient;
    private readonly ILogger<ApplyCommand> _logger;

    public ApplyCommand(
        ISchemaManager schemaManager,
        SchemaClient schemaClient,
        ILogger<ApplyCommand> logger)
        : base(CommandNames.Apply, Resources.ApplyCommandDescription)
    {
        AddOption(CommandOptions.ConnectionStringOption());
        AddOption(CommandOptions.ServerOption());
        AddOption(CommandOptions.VersionOption());
        AddOption(CommandOptions.NextOption());
        AddOption(CommandOptions.LatestOption());
        AddOption(CommandOptions.ForceOption());

        Handler = CommandHandler.Create(
            (string connectionString, Uri server, MutuallyExclusiveType type, bool force, CancellationToken token)
            => HandlerAsync(connectionString, server, type, force, token));

        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ConnectionStringOption(), Resources.ConnectionStringRequiredValidation));
        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));
        Argument.AddValidator(symbol => MutuallyExclusiveOptionValidator.Validate(symbol, new List<Option> { CommandOptions.VersionOption(), CommandOptions.NextOption(), CommandOptions.LatestOption() }, Resources.MutuallyExclusiveValidation));

        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));
        EnsureArg.IsNotNull(schemaClient, nameof(schemaClient));

        _logger = logger;
        _schemaManager = schemaManager;
        _schemaClient = schemaClient;
    }

    private async Task HandlerAsync(string connectionString, Uri server, MutuallyExclusiveType exclusiveType, bool force, CancellationToken cancellationToken = default)
    {
        if (force && !EnsureForce())
        {
            return;
        }

        _schemaClient.SetUri(server);

        await _schemaManager.ApplySchema(connectionString, exclusiveType, cancellationToken).ConfigureAwait(false);
    }

    private bool EnsureForce()
    {
        _logger.LogWarning("Are you sure to apply command with force option? Type 'yes' to confirm.");
        return string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase);
    }
}
