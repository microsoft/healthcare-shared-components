// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using SchemaManager.Exceptions;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class BaseCommand
    {
        public static void Handler(string connectionString)
        {
            try
            {
                BaseSchemaUtils.EnsureBaseSchemaExists(connectionString);
            }
            catch (Exception ex) when (ex is SchemaManagerException || ex is InvalidOperationException)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
        }
    }
}