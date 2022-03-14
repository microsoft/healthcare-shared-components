// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql;

/// <summary>
/// Visits a SQL AST and creates a class for each CREATE TABLE, CREATE VIEW, and CREATE INDEX statement.
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
            CreateSkeletalClass(className, schemaQualifiedTableName)
                .AddMembers(node.Definition.ColumnDefinitions.Select(CreatePropertyForTableColumn).ToArray());

        FieldDeclarationSyntax field = CreateStaticFieldForClass(className, tableName);

        MembersToAdd.Add(field.AddSortingKey(this, tableName));
        MembersToAdd.Add(classDeclarationSyntax.AddSortingKey(this, tableName));

        base.Visit(node);
    }

    private static ClassDeclarationSyntax CreateSkeletalClass(string className, string schemaQualifiedTableName)
    {
        return ClassDeclaration(className)
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
                    .WithBody(Block()));
    }

    public override void Visit(CreateViewStatement node)
    {
        // Determining the column types of a view is a bit more involved.
        // We need to determine which tables are being queried and look up the
        // columns that the query references.

        // This is a basic implementation that can be enhanced as needed to accomodate more scenarios.
        // It assumes that any table referenced in the view are declared before the view.

        string viewName = node.SchemaObjectName.BaseIdentifier.Value;
        string schemaQualifiedViewName = $"{node.SchemaObjectName.SchemaIdentifier.Value}.{viewName}";
        string className = GetClassNameForView(viewName);

        var querySpecification = (QuerySpecification)node.SelectStatement.QueryExpression;
        IList<SelectElement> selectElements = querySpecification.SelectElements;

        List<(string name, string alias)> tables =
            querySpecification.FromClause.TableReferences
                .SelectMany(tr => tr switch
                {
                    JoinTableReference @join => new[] { @join.FirstTableReference, @join.SecondTableReference },
                    _ => new[] { tr },
                })
                .Select(tr => tr switch
                {
                    NamedTableReference ntr => (ntr.SchemaObject.BaseIdentifier.Value, ntr.Alias.Value),
                    _ => throw new NotSupportedException($"Unrecognized table type '{tr.GetType().Name}' in view.")
                }).ToList();

        ClassDeclarationSyntax classDeclarationSyntax =
            CreateSkeletalClass(className, schemaQualifiedViewName)
                .AddMembers(selectElements.Select(selectElement => CreatePropertyForViewColumn(selectElement, tables)).ToArray());

        FieldDeclarationSyntax field = CreateStaticFieldForClass(className, viewName);

        MembersToAdd.Add(field.AddSortingKey(this, viewName));
        MembersToAdd.Add(classDeclarationSyntax.AddSortingKey(this, viewName));

        base.Visit(node);
    }

    private MemberDeclarationSyntax CreatePropertyForViewColumn(SelectElement selectElement, List<(string name, string alias)> tablesInScope)
    {
        if (selectElement is not SelectScalarExpression exp)
        {
            // notably SELECT * is not supported
            throw new NotSupportedException($"Select element {selectElement.GetType().Name} is not supported");
        }

        if (exp.Expression is not ColumnReferenceExpression columnReference)
        {
            throw new NotSupportedException($"{exp.Expression.GetType().Name} is not supported.");
        }

        if (columnReference.MultiPartIdentifier.Count != 2)
        {
            throw new NotSupportedException("Please qualify column references with the table's alias");
        }

        string tableAliasName = columnReference.MultiPartIdentifier[0].Value;
        string tableColumnName = columnReference.MultiPartIdentifier[1].Value;

        string tableName = tablesInScope.Where(t => t.alias == tableAliasName).Select(t => t.name).FirstOrDefault() ?? throw new InvalidOperationException($"Unable to resolve table alias '{tableAliasName}'.");

        string classNameForTable = GetClassNameForTable(tableName);

        // find the class we generated for the table
        var tableDeclaration = (ClassDeclarationSyntax)MembersToAdd.FirstOrDefault(m => m is ClassDeclarationSyntax c && c.Identifier.ValueText == classNameForTable)
                               ?? throw new InvalidOperationException($"Table '{classNameForTable}' was not found");

        // find the field we generated for the column
        var columnDeclaration = (FieldDeclarationSyntax)tableDeclaration.Members.FirstOrDefault(m => m is FieldDeclarationSyntax fd && fd.Declaration.Variables[0].Identifier.ValueText == tableColumnName)
                                ?? throw new InvalidOperationException($"Unable to find member for column '{tableColumnName}'");

        if (exp.ColumnName == null)
        {
            // we can reuse the same declaration for the view.
            return columnDeclaration;
        }

        // In this scenario, we have a SELECT t.MyCol AS Abc. We need to change the column from "MyCol" to "Abc"

        return columnDeclaration.ReplaceNodes(
            columnDeclaration.DescendantNodes(),
            (original, updated) => original switch
            {
                VariableDeclaratorSyntax v when v.Identifier.ValueText == tableColumnName => ((VariableDeclaratorSyntax)updated).WithIdentifier(Identifier(exp.ColumnName.Value)),
                LiteralExpressionSyntax l when l.Token.ValueText == tableColumnName => ((LiteralExpressionSyntax)updated).Update(Literal(exp.ColumnName.Value)),
                _ => updated,
            });
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
        string viewClassName = GetClassNameForView(node.OnName.BaseIdentifier.Value);

        var memberIndex = MembersToAdd.FindIndex(m => m is ClassDeclarationSyntax c && (c.Identifier.ValueText == tableClassName || c.Identifier.ValueText == viewClassName));

        if (memberIndex < 0)
        {
            throw new InvalidOperationException($"Index '{node.Name.Value}' is declared on an unrecognized type '{node.OnName.BaseIdentifier.Value}'");
        }

        MembersToAdd[memberIndex] = ((ClassDeclarationSyntax)MembersToAdd[memberIndex]).AddMembers(indexNameField);

        base.Visit(node);
    }

    private static string GetClassNameForTable(string tableName) => $"{tableName}Table";

    private static string GetClassNameForView(string viewName) => $"{viewName}View";
}
