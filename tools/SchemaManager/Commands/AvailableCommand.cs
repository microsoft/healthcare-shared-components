﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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

public class AvailableCommand : Command
{
    private readonly ISchemaManager _schemaManager;
    private readonly SchemaClient _schemaClient;

    public AvailableCommand(ISchemaManager schemaManager, SchemaClient schemaClient)
        : base(CommandNames.Available, Resources.AvailableCommandDescription)
    {
        AddOption(CommandOptions.ServerOption());

        Handler = CommandHandler.Create(
            (InvocationContext context, Uri server, CancellationToken token)
            => HandlerAsync(context, server, token));

        Argument.AddValidator(symbol => RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));

        EnsureArg.IsNotNull(schemaManager, nameof(schemaManager));
        EnsureArg.IsNotNull(schemaClient, nameof(schemaClient));

        _schemaManager = schemaManager;
        _schemaClient = schemaClient;
    }

    private async Task HandlerAsync(InvocationContext invocationContext, Uri server, CancellationToken cancellationToken)
    {
        _schemaClient.SetUri(server);

        var availableVersions = await _schemaManager.GetAvailableSchema(cancellationToken);

        var region = new Region(
            0,
            0,
            Console.WindowWidth,
            Console.WindowHeight,
            true);

        var tableView = new TableView<AvailableVersion>
        {
            Items = new ReadOnlyCollection<AvailableVersion>(availableVersions),
        };

        tableView.AddColumn(
          cellValue: availableVersion => availableVersion.Id,
          header: new ContentView("Version"));

        tableView.AddColumn(
            cellValue: availableVersion => availableVersion.ScriptUri,
            header: new ContentView("Script"));

        tableView.AddColumn(
            cellValue: availableVersion => string.IsNullOrEmpty(availableVersion.DiffUri) ? "N/A" : availableVersion.DiffUri,
            header: new ContentView("Diff"));

        var consoleRenderer = new ConsoleRenderer(
            invocationContext.Console,
            mode: invocationContext.BindingContext.OutputMode(),
            resetAfterRender: true);

        using (var screen = new ScreenView(renderer: consoleRenderer))
        {
            screen.Child = tableView;
            screen.Render(region);
        }
    }
}
