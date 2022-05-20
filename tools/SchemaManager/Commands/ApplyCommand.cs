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
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using SchemaManager.Validators;

namespace SchemaManager.Commands;

public class ApplyCommand : Command
{
    private readonly ISchemaManager _schemaManager;
    private readonly ILogger<ApplyCommand> _logger;

    public ApplyCommand(
        ISchemaManager schemaManager,
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
            (MutuallyExclusiveType type, bool force, CancellationToken token)
            => ApplyHandler(type, force, token));

        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ConnectionStringOption(), Resources.ConnectionStringRequiredValidation));
        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));
        Argument.AddValidator(symbol => MutuallyExclusiveOptionValidator.Validate(symbol, new List<Option> { CommandOptions.VersionOption(), CommandOptions.NextOption(), CommandOptions.LatestOption() }, Resources.MutuallyExclusiveValidation));

        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));

        _logger = logger;
        _schemaManager = schemaManager;
    }

    private Task ApplyHandler(MutuallyExclusiveType exclusiveType, bool force, CancellationToken cancellationToken = default)
    {
        if (force && !EnsureForce())
        {
            return Task.CompletedTask;
        }

        return _schemaManager.ApplySchema(exclusiveType, cancellationToken);
    }

    private bool EnsureForce()
    {
        _logger.LogWarning("Are you sure to apply command with force option? Type 'yes' to confirm.");
        return string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase);
    }
}
