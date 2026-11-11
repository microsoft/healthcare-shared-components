// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator;

internal static class ParameterExtensions
{
    public static TypeSyntax ToTypeSyntax(this ParameterInfo p, bool useGlobalAlias = false)
    {
        // The ref kind is expressed as a parameter modifier (see GetRefKindKeyword) rather than as part of the type
        Type type = p.GetRefKind() is RefKind.None ? p.ParameterType : p.ParameterType.GetElementType();
        return type.ToTypeSyntax(useGlobalAlias);
    }

    public static RefKind GetRefKind(this ParameterInfo p)
    {
        if (!p.ParameterType.IsByRef)
            return RefKind.None;

        if (p.IsOut && !p.IsIn)
            return RefKind.Out;

        // 'in' and 'ref readonly' parameters are distinguished from 'ref' by compiler-reserved attributes rather than by [In]
        if (p.CustomAttributes.Any(x => x.AttributeType == typeof(RequiresLocationAttribute)))
            throw new NotSupportedException($"Parameter '{p.Name}' of '{p.Member.DeclaringType}.{p.Member.Name}' is a 'ref readonly' parameter. Only 'ref', 'out', and 'in' by-ref parameters are supported.");

        return p.CustomAttributes.Any(x => x.AttributeType == typeof(IsReadOnlyAttribute)) ? RefKind.In : RefKind.Ref;
    }

    public static SyntaxToken GetRefKindKeyword(this ParameterInfo p)
    {
        return p.GetRefKind() switch
        {
            RefKind.Ref => SyntaxFactory.Token(SyntaxKind.RefKeyword),
            RefKind.Out => SyntaxFactory.Token(SyntaxKind.OutKeyword),
            RefKind.In => SyntaxFactory.Token(SyntaxKind.InKeyword),
            _ => default,
        };
    }

    public static SyntaxTokenList GetRefKindModifiers(this ParameterInfo p)
    {
        SyntaxToken keyword = p.GetRefKindKeyword();
        return keyword.IsKind(SyntaxKind.None) ? SyntaxFactory.TokenList() : SyntaxFactory.TokenList(keyword);
    }

    public static ParameterSyntax WithOptionalAttributeLists(this ParameterSyntax parameter, IEnumerable<CustomAttributeData> attributes)
    {
        // Skip internal attributes which are not meant to be used by developers
        // but may be discovered using reflection.
        // TODO: Enable nullability attributes once the repository uses a nullable-aware context
        List<CustomAttributeData> eligibleAttributes = attributes
            .Where(x =>
            {
                // Used internally to record whether a reference type may be nullable. Developers should use '?' instead
                return x.AttributeType != typeof(NullableAttribute) &&

                    // Used internally for "in" parameters. Developers should use the 'in' modifier instead
                    x.AttributeType != typeof(IsReadOnlyAttribute) &&

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
