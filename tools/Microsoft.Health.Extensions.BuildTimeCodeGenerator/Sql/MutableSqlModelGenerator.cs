// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql;

/// <summary>
/// Generates a class with members based on CREATE TABLE, CREATE PROCEDURE AND CREATE OR ALTER PROCEDURE statements in a .sql file.
/// </summary>
public class MutableSqlModelGenerator : SqlModelGenerator
{
    public MutableSqlModelGenerator(string[] args)
        : base(args)
    {
        if (args.Length != 1)
        {
            throw new InvalidOperationException($"Only one file is supported as an argument for {nameof(MutableSqlModelGenerator)}");
        }
    }

    protected override SqlVisitor[] Visitors => new SqlVisitor[] { new CreateTableVisitor(), new CreateProcedureVisitor(), new CreateOrAlterProcedureVisitor() };

    protected override MemberDeclarationSyntax[] WrapMembers(MemberDeclarationSyntax[] members, string containingTypeName)
    {
        MemberDeclarationSyntax classDeclarationSyntax = ClassDeclaration(containingTypeName)
            .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
            .AddMembers(members);

        return new[] { classDeclarationSyntax };
    }
}
