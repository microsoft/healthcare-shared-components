// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using SchemaManager.Core;
using SchemaManager.Validators;

namespace SchemaManager.Commands;

public class CurrentCommand : Command
{
    private readonly ISchemaManager _schemaManager;

    public CurrentCommand(ISchemaManager schemaManager)
        : base(CommandNames.Current, Resources.CurrentCommandDescription)
    {
        AddOption(CommandOptions.ServerOption());
        AddOption(CommandOptions.ConnectionStringOption());

        Handler = CommandHandler.Create(
            (InvocationContext context, CancellationToken token)
            => HandlerAsync(context, token));

        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ConnectionStringOption(), Resources.ConnectionStringRequiredValidation));
        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));

        EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));

        _schemaManager = schemaManager;
    }

    private async Task HandlerAsync(InvocationContext invocationContext, CancellationToken cancellationToken = default)
    {
        var region = new Region(
                      0,
                      0,
                      Console.WindowWidth,
                      Console.WindowHeight,
                      true);

        IList<CurrentVersion> currentVersions = await _schemaManager.GetCurrentSchema(cancellationToken).ConfigureAwait(false);

        var tableView = new TableView<CurrentVersion>
        {
            Items = new ReadOnlyCollection<CurrentVersion>(currentVersions),
        };

        tableView.AddColumn(
           cellValue: currentVersion => currentVersion.Id,
           header: new ContentView("Version"));

        tableView.AddColumn(
            cellValue: currentVersion => currentVersion.Status,
            header: new ContentView("Status"));

        tableView.AddColumn(
            cellValue: currentVersion => string.Join(", ", currentVersion.Servers),
            header: new ContentView("Servers"));

        var consoleRenderer = new ConsoleRenderer(
            invocationContext.Console,
            mode: invocationContext.BindingContext.OutputMode(),
            resetAfterRender: true);

        using var screen = new ScreenView(renderer: consoleRenderer) { Child = tableView };
        screen.Render(region);
    }
}
