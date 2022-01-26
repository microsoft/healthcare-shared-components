﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator
{
    /// <summary>
    /// Generates a class that wraps <see cref="StringBuilder"/> and provides methods for prefixing lines with an indent level.
    /// </summary>
    internal class IndentedStringBuilderGenerator : ICodeGenerator
    {
        public IndentedStringBuilderGenerator(string[] args)
        {
        }

        (MemberDeclarationSyntax[], UsingDirectiveSyntax[]) ICodeGenerator.Generate(string typeName)
        {
            // start off by generating a class that has the same API shape as StringBuilder and that forwards calls to an inner
            // StringBuilder. Reuse DelegatingInterfaceImplementationGenerator even though StringBuilder is not an interface.
            // We'll patch things up after.

            var delegatingGenerator = new DelegatingInterfaceImplementationGenerator(
                typeModifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword)),
                constructorModifiers: TokenList(Token(SyntaxKind.PublicKeyword)),
                typeof(StringBuilder));

            (MemberDeclarationSyntax[] declarations, UsingDirectiveSyntax[] usings) = delegatingGenerator.Generate(typeName);

            SyntaxNode syntaxNode = new Rewriter().Visit(declarations.Single().SyntaxTree.GetRoot());

            return (new[] { (MemberDeclarationSyntax)syntaxNode }, usings);
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return base.VisitClassDeclaration(node.WithBaseList(null));
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                return node
                    .WithExplicitInterfaceSpecifier(null)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));
            }

            public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
            {
                return node
                    .WithExplicitInterfaceSpecifier(null)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.ReturnType is PointerTypeSyntax || node.ParameterList.Parameters.Any(p => p.Type is PointerTypeSyntax))
                {
                    // skip unsafe methods
                    return IncompleteMember();
                }

                if (node.ParameterList.Parameters.Any(p => p.Identifier.ValueText == "handler"))
                {
                    // skip methods that use the AppendInterpolatedStringHandler
                    return IncompleteMember();
                }

                node = node
                    .WithExplicitInterfaceSpecifier(null)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));

                string methodName = node.Identifier.ToString();
                if (IsDeclaredOnObject(node))
                {
                    if (methodName == nameof(object.GetType))
                    {
                        // don't implement object.GetType()
                        return IncompleteMember();
                    }

                    node = node.AddModifiers(Token(SyntaxKind.OverrideKeyword));
                }

                if (node.ReturnType.ToString() == typeof(StringBuilder).FullName)
                {
                    // return this instead of the inner StringBuilder that is returned by the inner call.
                    node = node.WithReturnType(IdentifierName("IndentedStringBuilder"));
                    ExpressionSyntax invocation = ((ReturnStatementSyntax)node.Body.Statements.Single()).Expression;
                    node = node.WithBody(Block(ExpressionStatement(invocation), ReturnStatement(ThisExpression())));
                }

                if (methodName.StartsWith("Append", StringComparison.OrdinalIgnoreCase))
                {
                    // Add a call to AppendIndent at the start of the body
                    BlockSyntax body = node.Body.WithStatements(node.Body.Statements.Insert(0, ExpressionStatement(InvocationExpression(IdentifierName("AppendIndent")))));

                    if (methodName.EndsWith("Line", StringComparison.OrdinalIgnoreCase))
                    {
                        // before returning, set _indentPending to true
                        ExpressionStatementSyntax updatePendingStatement = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("_indentPending"), LiteralExpression(SyntaxKind.TrueLiteralExpression)));

                        body = body.InsertNodesBefore(body.Statements.Single(s => s is ReturnStatementSyntax), new SyntaxNode[] { updatePendingStatement });
                    }

                    node = node.WithBody(body);
                }

                return node;
            }

            private static bool IsDeclaredOnObject(SyntaxNode node)
            {
                return node.GetAnnotations(DelegatingInterfaceImplementationGenerator.DeclaringTypeKind).Single().Data == typeof(object).FullName;
            }
        }
    }
}
