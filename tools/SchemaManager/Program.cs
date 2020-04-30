// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using SchemaManager.Commands;
using SchemaManager.Model;

namespace SchemaManager
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serverOption = new Option(
                OptionAliases.Server,
                Resources.ServerOptionDescription,
                new Argument<Uri> { Arity = ArgumentArity.ExactlyOne });

            var connectionStringOption = new Option(
                OptionAliases.ConnectionString,
                Resources.ConnectionStringOptionDescription,
                new Argument<string> { Arity = ArgumentArity.ExactlyOne });

            var versionOption = new Option(
                OptionAliases.Version,
                Resources.VersionOptionDescription,
                new Argument<int> { Arity = ArgumentArity.ExactlyOne });

            versionOption.AddAlias(OptionAliases.V);

            var nextOption = new Option(
               OptionAliases.Next,
               Resources.NextOptionDescritpion,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            var latestOption = new Option(
               OptionAliases.Latest,
               Resources.LatestOptionDescription,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            var forceOption = new Option(
                OptionAliases.Force,
                Resources.ForceOptionDescription,
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });

            var rootCommand = new RootCommand();

            var currentCommand = new Command(CommandNames.Current, Resources.CurrentCommandDescription)
            {
                serverOption,
            };
            currentCommand.Handler = CommandHandler.Create<InvocationContext, Uri>(CurrentCommand.HandlerAsync);
            currentCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));

            var applyCommand = new Command(CommandNames.Apply, Resources.ApplyCommandDescription)
            {
                connectionStringOption,
                serverOption,
                versionOption,
                nextOption,
                latestOption,
                forceOption,
            };
            applyCommand.Handler = CommandHandler.Create<string, Uri, MutuallyExclusiveType, bool>(ApplyCommand.HandlerAsync);
            applyCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, connectionStringOption, Resources.ConnectionStringRequiredValidation));
            applyCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));
            applyCommand.Argument.AddValidator(symbol => Validators.MutuallyExclusiveOptionValidator.Validate(symbol, new List<Option> { versionOption, nextOption, latestOption }, Resources.MutuallyExclusiveValidation));

            var availableCommand = new Command(CommandNames.Available, Resources.AvailableCommandDescription)
            {
                serverOption,
            };
            availableCommand.Handler = CommandHandler.Create<InvocationContext, Uri>(AvailableCommand.Handler);
            availableCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));

            rootCommand.AddCommand(applyCommand);
            rootCommand.AddCommand(availableCommand);
            rootCommand.AddCommand(currentCommand);

            rootCommand.InvokeAsync(args).Wait();
        }
    }
}
