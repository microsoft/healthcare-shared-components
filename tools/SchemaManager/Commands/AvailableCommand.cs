// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public class AvailableCommand : Command
    {
        private readonly ISchemaClient _schemaClient;

        public AvailableCommand(ISchemaClient schemaClient)
            : base(CommandNames.Available, Resources.AvailableCommandDescription)
        {
            AddOption(CommandOptions.ServerOption());

            Handler = CommandHandler.Create(
                (InvocationContext context, Uri server, CancellationToken token)
                => HandlerAsync(context, server, token));

            Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));

            EnsureArg.IsNotNull(schemaClient);

            _schemaClient = schemaClient;
        }

        private async Task HandlerAsync(InvocationContext invocationContext, Uri server, CancellationToken cancellationToken)
        {
            var region = new Region(
                0,
                0,
                Console.WindowWidth,
                Console.WindowHeight,
                true);

            List<AvailableVersion> availableVersions = null;
            _schemaClient.SetUri(server);

            try
            {
                availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken);

                // To ensure that schema version null/0 is not printed
                if (availableVersions.First().Id == 0)
                {
                    availableVersions.RemoveAt(0);
                }
            }
            catch (SchemaManagerException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
            catch (HttpRequestException)
            {
                CommandUtils.PrintError(string.Format(Resources.RequestFailedMessage, server));
                return;
            }

            var tableView = new TableView<AvailableVersion>
            {
                Items = availableVersions,
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
}
