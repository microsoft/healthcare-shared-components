﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Client.Authentication;

public class NamedCredentialProvider : ICredentialProvider
{
    private readonly ICredentialProvider _credentialProvider;

    public NamedCredentialProvider(string name, ICredentialProvider credentialProvider)
    {
        EnsureArg.IsNotNull(name, nameof(name));
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));

        Name = name;
        _credentialProvider = credentialProvider;
    }

    public string Name { get; }

    public Task<string> GetBearerTokenAsync(CancellationToken cancellationToken)
    {
        return _credentialProvider.GetBearerTokenAsync(cancellationToken);
    }
}
