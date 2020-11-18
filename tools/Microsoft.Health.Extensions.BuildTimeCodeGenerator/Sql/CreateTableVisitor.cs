// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql
{
    /// <summary>
    /// Visits a SQL AST, created a class for each CREATE TABLE statement.
    /// </summary>
    internal class CreateTableVisitor : SqlVisitor
    {
        public override int ArtifactSortOder => 0;

        public override void Visit(CreateTableStatement node)
        {
            string tableName = node.SchemaObjectName.BaseIdentifier.Value;
            string schemaQualifiedTableName = $"{node.SchemaObjectName.SchemaIdentifier.Value}.{tableName}";
            string className = GetClassNameForTable(tableName);

            ClassDeclarationSyntax classDeclarationSyntax =
                ClassDeclaration(className)
                    .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword)))
                    .WithBaseList(
                        BaseList(
                            SingletonSeparatedList<BaseTypeSyntax>(
                                SimpleBaseType(
                                    IdentifierName("Table")))))
                    .AddMembers(
                        ConstructorDeclaration(
                                Identifier(className))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.InternalKeyword)))
                            .WithInitializer(
                                ConstructorInitializer(
                                    SyntaxKind.BaseConstructorInitializer,
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(schemaQualifiedTableName)))))))
                            .WithBody(Block()))
                    .AddMembers(node.Definition.ColumnDefinitions.Select(CreatePropertyForColumn).ToArray());

            FieldDeclarationSyntax field = CreateStaticFieldForClass(className, tableName);

            MembersToAdd.Add(field.AddSortingKey(this, tableName));
            MembersToAdd.Add(classDeclarationSyntax.AddSortingKey(this, tableName));

            base.Visit(node);
        }

        public override void Visit(CreateIndexStatement node)
        {
            string indexName = node.Name.Value;

            var indexClassName = IdentifierName("Index");
            FieldDeclarationSyntax indexNameField =
                FieldDeclaration(
                        VariableDeclaration(indexClassName)
                            .AddVariables(
                                VariableDeclarator(Identifier(indexName))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            ObjectCreationExpression(indexClassName).AddArgumentListArguments(
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(indexName))))))))
                    .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword));

            string tableClassName = GetClassNameForTable(node.OnName.BaseIdentifier.Value);

            var memberIndex = MembersToAdd.FindIndex(m => m is ClassDeclarationSyntax c && c.Identifier.ValueText == tableClassName);

            if (memberIndex < 0)
            {
                throw new InvalidOperationException($"Index '{node.Name.Value}' is declared on an unrecognized type '{node.OnName.BaseIdentifier.Value}'");
            }

            MembersToAdd[memberIndex] = ((ClassDeclarationSyntax)MembersToAdd[memberIndex]).AddMembers(indexNameField);

            base.Visit(node);
        }

        private static string GetClassNameForTable(string tableName) => $"{tableName}Table";
    }
}