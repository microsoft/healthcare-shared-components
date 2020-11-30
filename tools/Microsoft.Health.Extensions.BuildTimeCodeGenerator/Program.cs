﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator
{
    internal class Program
    {
        private static HashSet<Assembly> cache = new HashSet<Assembly>();

        public static void Main(string generatorName, FileInfo outputFile, string @namespace, string[] args = null)
        {
            string className = Path.GetFileName(outputFile.Name).Split('.')[0];

            Type codeGeneratorType = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name == generatorName) ?? throw new ArgumentException($"Generator '{generatorName} not found");
            var generator = (ICodeGenerator)Activator.CreateInstance(codeGeneratorType, new object[] { args });

            (MemberDeclarationSyntax[] declarations, UsingDirectiveSyntax[] usingDirectives) = generator.Generate(className);

            NamespaceDeclarationSyntax namespaceDeclaration =
                NamespaceDeclaration(IdentifierName(@namespace))
                    .AddUsings(usingDirectives)
                    .AddMembers(declarations)
                    .WithLeadingTrivia(
                        Comment("//------------------------------------------------------------------------------"),
                        Comment("// <auto-generated>"),
                        Comment("//     This code was generated by a tool."),
                        Comment("//"),
                        Comment("//     Changes to this file may cause incorrect behavior and will be lost if"),
                        Comment("//     the code is regenerated."),
                        Comment("// </auto-generated>"),
                        Comment("//------------------------------------------------------------------------------"));

            File.WriteAllText(outputFile.FullName, namespaceDeclaration.NormalizeWhitespace().SyntaxTree.ToString());
        }

        private static IEnumerable<MetadataReference> GetClosure(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (!cache.Contains(assembly))
                {
                    yield return MetadataReference.CreateFromFile(assembly.Location);
                    cache.Add(assembly);
                    foreach (var metadataReference in GetClosure(assembly.GetReferencedAssemblies().Select(name =>
                    {
                        Console.WriteLine(name);
                        return Assembly.Load(name);
                    })))
                    {
                        yield return metadataReference;
                    }
                }
            }
        }
    }
}
