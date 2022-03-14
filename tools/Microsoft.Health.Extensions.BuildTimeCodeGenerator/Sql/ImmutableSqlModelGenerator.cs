// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql;

/// <summary>
/// Generates types based on CREATE TABLE TYPE statements in one or more .sql file.
/// Since these types are immutable (there is not ALTER command for them), table types with the same
/// name encountered across different .sql files are considered equivalent.
/// </summary>
public class ImmutableSqlModelGenerator : SqlModelGenerator
{
    public ImmutableSqlModelGenerator(string[] args)
        : base(args)
    {
    }

    protected override SqlVisitor[] Visitors => new SqlVisitor[] { new CreateTableTypeVisitor() };

    protected override MemberDeclarationSyntax[] WrapMembers(MemberDeclarationSyntax[] members, string containingTypeName)
    {
        return members;
    }
}
