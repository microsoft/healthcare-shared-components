// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator;

internal static class ParameterExtensions
{
    public static ParameterSyntax WithOptionalAttributeLists(this ParameterSyntax parameter, IEnumerable<CustomAttributeData> attributes)
    {
        // Skip internal attributes which are not meant to be used by developers
        // but may be discovered using reflection.
        // TODO: Enable nullability attributes once the repository uses a nullable-aware context
        List<CustomAttributeData> eligibleAttributes = attributes
            .Where(x =>
            {
                // Used internally to record whether a reference type may be nullable. Developers should use '?' instead
                return x.AttributeType.FullName != "System.Runtime.CompilerServices.NullableAttribute" &&

                    // Used internally for "params" parameters
                    x.AttributeType != typeof(ParamArrayAttribute) &&

                    // Public attributes used by developers for aiding in static code analysis
                    x.AttributeType != typeof(AllowNullAttribute) &&
                    x.AttributeType != typeof(DisallowNullAttribute) &&
                    x.AttributeType != typeof(MaybeNullAttribute) &&
                    x.AttributeType != typeof(NotNullAttribute) &&
                    x.AttributeType != typeof(MaybeNullWhenAttribute) &&
                    x.AttributeType != typeof(NotNullWhenAttribute) &&
                    x.AttributeType != typeof(NotNullIfNotNullAttribute) &&
                    x.AttributeType != typeof(DoesNotReturnIfAttribute);
            })
            .ToList();

        return eligibleAttributes.Count > 0 ? parameter.WithAttributeLists(GetAttributeList(eligibleAttributes)) : parameter;
    }

    private static SyntaxList<AttributeListSyntax> GetAttributeList(this IEnumerable<CustomAttributeData> attributes)
    {
        return SyntaxFactory.SingletonList(
            SyntaxFactory.AttributeList(
                SyntaxFactory.SeparatedList(
                    attributes.Select(x => SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName(x.AttributeType.FullName),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SeparatedList(
                                x.ConstructorArguments
                                    .SelectMany(GetConstructorArguments)
                                    .Concat(x.NamedArguments.Select(GetNamedAttributeArgument)))))))));
    }

    private static IEnumerable<AttributeArgumentSyntax> GetConstructorArguments(CustomAttributeTypedArgument arg)
    {
        // First check if this is a params ctor argument (cannot be nested)
        if (arg.ArgumentType.IsArray && arg.Value != null)
        {
            foreach (CustomAttributeTypedArgument element in arg.Value as IEnumerable<CustomAttributeTypedArgument>)
            {
                yield return SyntaxFactory.AttributeArgument(default, default, GetExpression(element.Value));
            }
        }
        else
        {
            yield return SyntaxFactory.AttributeArgument(default, default, GetExpression(arg.Value));
        }
    }

    private static AttributeArgumentSyntax GetNamedAttributeArgument(CustomAttributeNamedArgument arg)
    {
        if (arg.IsField)
        {
            return SyntaxFactory.AttributeArgument(
                SyntaxFactory.NameEquals(
                    SyntaxFactory.IdentifierName(arg.MemberName),
                    SyntaxFactory.Token(SyntaxKind.EqualsToken)),
                default,
                GetExpression(arg.TypedValue.Value));
        }
        else
        {
            return SyntaxFactory.AttributeArgument(
                default,
                SyntaxFactory.NameColon(
                    SyntaxFactory.IdentifierName(arg.MemberName),
                    SyntaxFactory.Token(SyntaxKind.ColonToken)),
                GetExpression(arg.TypedValue.Value));
        }
    }

    private static ExpressionSyntax GetExpression(object value)
    {
        return value switch
        {
            string str => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(str)),
            char c => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(c)),
            byte b => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(b)),
            sbyte sb => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(sb)),
            short s => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(s)),
            ushort us => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(us)),
            int i => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i)),
            uint ui => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ui)),
            long l => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(l)),
            ulong ul => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ul)),
            float f => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(f)),
            double d => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(d)),
            decimal m => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(m)),
            bool b when b => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, SyntaxFactory.Token(SyntaxKind.TrueKeyword)),
            bool b when !b => SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword)),
            null => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)),
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };
    }
}
