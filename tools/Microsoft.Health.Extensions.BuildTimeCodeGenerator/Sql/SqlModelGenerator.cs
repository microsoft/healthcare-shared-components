// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnsureThat;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql
{
    /// <summary>
    /// Base class for generating C# types based on the CREATE TABLE, CREATE TABLE TYPE, and CREATE PROCEDURE statements in one or more .sql files.
    /// </summary>
    public abstract class SqlModelGenerator : ICodeGenerator
    {
        private readonly string[] _sqlFiles;

        protected SqlModelGenerator(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            _sqlFiles = args;
        }

        protected abstract SqlVisitor[] Visitors { get; }

        public (MemberDeclarationSyntax[], UsingDirectiveSyntax[]) Generate(string typeName)
        {
            IEnumerable<TSqlFragment> sqlFragments = ParseSqlFiles();

            var visitors = Visitors;

            foreach (TSqlFragment sqlFragment in sqlFragments)
            {
                foreach (var sqlVisitor in visitors)
                {
                    sqlFragment.Accept(sqlVisitor);
                }
            }

            MemberDeclarationSyntax[] members = visitors
                .SelectMany(v => v.MembersToAdd)
                .OrderBy(m => m, MemberSorting.Comparer)
                .ToArray();

            return (
                WrapMembers(members, typeName),
                new[]
                {
                    UsingDirective(ParseName("Microsoft.Health.SqlServer.Features.Client")),
                    UsingDirective(ParseName("Microsoft.Health.SqlServer.Features.Schema.Model")),
                });
        }

        protected abstract MemberDeclarationSyntax[] WrapMembers(MemberDeclarationSyntax[] members, string containingTypeName);

        private IEnumerable<TSqlFragment> ParseSqlFiles()
        {
            foreach (var sqlFile in _sqlFiles)
            {
                using (var stream = File.OpenRead(sqlFile))
                using (var reader = new StreamReader(stream))
                {
                    var parser = new TSql150Parser(true);
                    yield return parser.Parse(reader, out var errors);
                }
            }
        }
    }
}