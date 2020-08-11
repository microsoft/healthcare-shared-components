// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Net.Http;
using System.Threading.Tasks;
using SchemaManager.Exceptions;
using SchemaManager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class AvailableCommand
    {
        public static async Task Handler(InvocationContext invocationContext, Uri server)
        {
            var region = new Region(
                0,
                0,
                Console.WindowWidth,
                Console.WindowHeight,
                true);

            List<AvailableVersion> availableVersions = null;
            ISchemaClient schemaClient = new SchemaClient(server);

            try
            {
                availableVersions = await schemaClient.GetAvailability();
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
