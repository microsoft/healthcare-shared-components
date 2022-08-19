// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace SchemaManager;

public class CommandLineOptions
{
    public Uri Server { get; set; }

    public string ConnectionString { get; set; }
}
