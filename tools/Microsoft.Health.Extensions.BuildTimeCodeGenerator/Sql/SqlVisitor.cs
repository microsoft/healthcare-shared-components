// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Health.Extensions.BuildTimeCodeGenerator.Sql;

/// <summary>
/// The base class for SQL visitors
/// </summary>
public abstract class SqlVisitor : TSqlFragmentVisitor
{
    private static readonly HashSet<string> Keywords = new HashSet<string>() { "TableName" };

    /// <summary>
    /// These members will be added to the generated class.
    /// </summary>
    public List<MemberDeclarationSyntax> MembersToAdd { get; } = new List<MemberDeclarationSyntax>();

    public abstract int ArtifactSortOder { get; }

    /// <summary>
    /// Converts a <see cref="DataTypeReference"/> to a <see cref="TypeSyntax"/>
    /// </summary>
    /// <param name="dataType">The type</param>
    /// <param name="nullable">Whether the type is nullable</param>
    /// <returns>The <see cref="TypeSyntax"/></returns>
    protected static TypeSyntax DataTypeReferenceToClrType(DataTypeReference dataType, bool nullable)
    {
        if (Enum.TryParse<SqlDbType>(dataType.Name.BaseIdentifier.Value, ignoreCase: true, out var sqlDbType))
        {
            return SqlDbTypeToClrType(sqlDbType, nullable).ToTypeSyntax(useGlobalAlias: true);
        }

        // assumed to be a table type

        return TypeExtensions.CreateGenericTypeFromGenericTypeDefinition(
            typeof(IEnumerable<>).ToTypeSyntax(true),
            IdentifierName(GetRowStructNameForTableType(dataType.Name)));
    }

    /// <summary>
    /// Converts a <see cref="SqlDbType"/> to a <see cref="Type"/>.
    /// </summary>
    /// <param name="sqlDbType">The type</param>
    /// <param name="nullable">Whether the type is nullable</param>
    /// <returns>The <see cref="Type"/></returns>
    protected static Type SqlDbTypeToClrType(SqlDbType sqlDbType, bool nullable)
    {
        Type type = SqlDbTypeToClrType(sqlDbType);
        if (nullable && !type.IsClass)
        {
            type = typeof(Nullable<>).MakeGenericType(type);
        }

        return type;
    }

    /// <summary>
    /// Converts a <see cref="SqlDbType"/> to a <see cref="Type"/>.
    /// </summary>
    /// <param name="sqlDbType">The type</param>
    /// <returns>The <see cref="Type"/></returns>
    protected static Type SqlDbTypeToClrType(SqlDbType sqlDbType)
    {
        switch (sqlDbType)
        {
            case SqlDbType.BigInt:
                return typeof(long);
            case SqlDbType.Binary:
                return typeof(byte[]);
            case SqlDbType.Bit:
                return typeof(bool);
            case SqlDbType.Char:
                break;
            case SqlDbType.Date:
            case SqlDbType.DateTime:
            case SqlDbType.DateTime2:
            case SqlDbType.SmallDateTime:
            case SqlDbType.Time:
                return typeof(DateTime);
            case SqlDbType.DateTimeOffset:
                return typeof(DateTimeOffset);
            case SqlDbType.Decimal:
                return typeof(decimal);
            case SqlDbType.Float:
                return typeof(double);
            case SqlDbType.Image:
                break;
            case SqlDbType.Int:
                return typeof(int);
            case SqlDbType.Money:
                break;
            case SqlDbType.NChar:
            case SqlDbType.NText:
            case SqlDbType.VarChar:
            case SqlDbType.NVarChar:
            case SqlDbType.Text:
                return typeof(string);
            case SqlDbType.Real:
                return typeof(double);
            case SqlDbType.SmallInt:
                return typeof(short);
            case SqlDbType.SmallMoney:
                break;
            case SqlDbType.Structured:
                return typeof(object);
            case SqlDbType.Timestamp:
                return typeof(byte[]);
            case SqlDbType.TinyInt:
                return typeof(byte);
            case SqlDbType.Udt:
                break;
            case SqlDbType.UniqueIdentifier:
                return typeof(Guid);
            case SqlDbType.VarBinary:
                return typeof(Stream);
            case SqlDbType.Variant:
                break;
            case SqlDbType.Xml:
                break;
        }

        throw new NotSupportedException(sqlDbType.ToString());
    }

    /// <summary>
    /// Determines whether the given <see cref="ColumnDefinition"/> is nullable
    /// </summary>
    /// <param name="column">The column</param>
    /// <returns>Whether the column is nullable</returns>
    protected static bool IsColumnNullable(ColumnDefinition column)
    {
        return column.Constraints.Any(c => c is NullableConstraintDefinition nc && nc.Nullable);
    }

    /// <summary>
    /// Creates an internal static readonly field for an instance of a class that has a parameterless constructor
    /// </summary>
    /// <param name="className">The class name</param>
    /// <param name="fieldName">The field name</param>
    /// <returns>The field.</returns>
    protected static FieldDeclarationSyntax CreateStaticFieldForClass(string className, string fieldName)
    {
        return FieldDeclaration(
                VariableDeclaration(IdentifierName(className))
                    .AddVariables(VariableDeclarator(fieldName)
                        .WithInitializer(
                            EqualsValueClause(
                                ObjectCreationExpression(
                                    IdentifierName(className)).AddArgumentListArguments()))))
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword), Token(SyntaxKind.StaticKeyword));
    }

    /// <summary>
    /// Gets the arguments for SQL database types, like the length, precision, and scale.
    /// </summary>
    /// <param name="dataType">The column data type</param>
    /// <param name="collation">The column collation, if any</param>
    /// <returns>The type arguments</returns>
    protected static IEnumerable<ArgumentSyntax> GetDataTypeSpecificConstructorArguments(DataTypeReference dataType, Identifier collation)
    {
        if (dataType is ParameterizedDataTypeReference parameterizedDataType)
        {
            foreach (var parameter in parameterizedDataType.Parameters)
            {
                yield return Argument(
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(parameter.LiteralType == LiteralType.Max ? -1 : int.Parse(parameter.Value, CultureInfo.InvariantCulture))));
            }
        }

        if (collation != null)
        {
            yield return Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(collation.Value)));
        }
    }

    /// <summary>
    /// Gets the name of the class that derives from TableValuedParameterDefiniition for a table type.
    /// </summary>
    /// <param name="objectName">The name of the table type</param>
    /// <returns>The class name</returns>
    protected static string GetClassNameForTableValuedParameterDefinition(SchemaObjectName objectName)
    {
        return $"{GetTableTypeMemberName(objectName)}TableValuedParameterDefinition";
    }

    /// <summary>
    /// Gets the name of the row struct for a table type.
    /// </summary>
    /// <param name="objectName">The name of the table type</param>
    /// <returns>The struct name</returns>
    protected static string GetRowStructNameForTableType(SchemaObjectName objectName)
    {
        return $"{GetTableTypeMemberName(objectName)}Row";
    }

    /// <summary>
    /// Strips away the underscore in a in a suffix for a table type name and replaces it with "V"
    /// For example, MyTableType_1 will return MyTableTypeV1
    /// </summary>
    /// <param name="objectName">The table type name</param>
    /// <returns>The name</returns>
    private static string GetTableTypeMemberName(SchemaObjectName objectName)
    {
        return Regex.Replace(objectName.BaseIdentifier.Value, @"_(\d+)$", "V$1");
    }

    /// <summary>
    /// Strips away the version suffix for a member name.
    /// For example, "MyProcedure_1" will return "MyProcedure"
    /// </summary>
    /// <param name="objectName">The object name</param>
    /// <returns>The name</returns>
    protected static string GetMemberNameWithoutVersionSuffix(SchemaObjectName objectName)
    {
        return Regex.Replace(objectName.BaseIdentifier.Value, @"_\d+$", string.Empty);
    }

    private static string GetColunmName(string sqlColumnName)
    {
        return Keywords.Contains(sqlColumnName) ? sqlColumnName + "_1" : sqlColumnName;
    }

    protected static MemberDeclarationSyntax CreatePropertyForTableColumn(ColumnDefinition column)
    {
        string normalizedSqlDbType = null;

        // Handle computed columns
        if (column.DataType == null && column.ComputedColumnExpression != null)
        {
            return FieldDeclaration(
                VariableDeclaration(SyntaxFactory.ParseTypeName("string"))
                    .AddVariables(VariableDeclarator($"{column.ColumnIdentifier.Value}")
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(column.ColumnIdentifier.Value))))))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ConstKeyword));
        }

        var sqlDbTypeToNormalize = column.DataType.Name.BaseIdentifier.Value;
        if (Enum.TryParse(sqlDbTypeToNormalize, true, out SqlDbType sqlDbType))
        {
            normalizedSqlDbType = sqlDbType.ToString();
        }
        else
        {
            if (string.Compare(sqlDbTypeToNormalize, "RowVersion", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // The timestamp data type is a synonym for the rowversion data type. Its syntax is now deprecated in SQL.
                // We are translating rowversion (used in the SQL code) to timestamp here because the SqlDbType enum only supports timestamp.
                normalizedSqlDbType = SqlDbType.Timestamp.ToString();
            }
        }

        IdentifierNameSyntax typeName = IdentifierName($"{(IsColumnNullable(column) ? "Nullable" : string.Empty)}{normalizedSqlDbType}Column");

        return FieldDeclaration(
                VariableDeclaration(typeName)
                    .AddVariables(VariableDeclarator(GetColunmName(column.ColumnIdentifier.Value))
                        .WithInitializer(
                            EqualsValueClause(
                                ObjectCreationExpression(
                                        typeName)
                                    .AddArgumentListArguments(
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(column.ColumnIdentifier.Value))))
                                    .AddArgumentListArguments(GetDataTypeSpecificConstructorArguments(column.DataType, column.Collation).ToArray())))))
            .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.ReadOnlyKeyword));
    }
}
