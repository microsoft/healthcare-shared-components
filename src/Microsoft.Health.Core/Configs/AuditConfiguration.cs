// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Exceptions;

namespace Microsoft.Health.Core.Configs;

public class AuditConfiguration
{
    public AuditConfiguration()
    { }

    public AuditConfiguration(string customAuditHeaderPrefix)
        => CustomAuditHeaderPrefix = customAuditHeaderPrefix;

    public string CustomAuditHeaderPrefix
    {
        get => field;
        set => field = !string.IsNullOrEmpty(value) ? value : throw new InvalidDefinitionException(Resources.CustomHeaderPrefixCannotBeEmpty);
    }
}
