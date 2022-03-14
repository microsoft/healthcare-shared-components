﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Abstractions.Exceptions;

public class UnsupportedMediaTypeException : MicrosoftHealthException
{
    public UnsupportedMediaTypeException(string message)
        : base(message)
    {
    }
}
