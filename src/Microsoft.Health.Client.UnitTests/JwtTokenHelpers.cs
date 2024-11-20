// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.Client.UnitTests;

internal static class JwtTokenHelpers
{
    internal static string GenerateToken(DateTime expiration)
    {
        const string issuer = "testIssuer";
        const string audience = "testAudience";

        var securityKey = new SymmetricSecurityKey(new byte[32]) { KeyId = "key" };
        var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256));
        var payload = new JwtPayload(issuer, audience, null, notBefore: DateTime.MinValue, expires: expiration);

        var secToken = new JwtSecurityToken(header, payload);
        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(secToken);
    }
}
