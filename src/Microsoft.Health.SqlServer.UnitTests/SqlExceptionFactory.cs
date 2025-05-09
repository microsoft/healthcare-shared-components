﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.UnitTests;

/// <summary>
/// Provides functionality to create an instance of <see cref="SqlException"/>.
/// </summary>
/// <remarks>
/// Because SqlException is marked as sealed with no public constructor, we need to
/// use reflection to be able to create an instance of it for unit testing.
/// </remarks>
internal static class SqlExceptionFactory
{
    private static readonly Func<SqlErrorCollection> SqlErrorCollectionConstructorDelegate = CreateSqlCollectionFactory();
    private static readonly Action<SqlErrorCollection, SqlError> SqlErrorCollectionAddDelegate = CreateAddErrorMethod();
    private static readonly Func<int, SqlError> SqlErrorConstructorDelegate = CreateSqlErrorFactory();
    private static readonly Func<string, SqlErrorCollection, Exception, Guid, SqlException> SqlExceptionConstructorDelegate = CreateSqlExceptionFactory();

    /// <summary>
    /// Creates a new instance of <see cref="SqlException"/>.
    /// </summary>
    /// <param name="number">The info number.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An instance of <see cref="SqlException"/>.</returns>
    public static SqlException Create(int number, string errorMessage = "Simulated exception.")
    {
        SqlError sqlError = SqlErrorConstructorDelegate(number);

        SqlErrorCollection sqlErrorCollection = SqlErrorCollectionConstructorDelegate();

        SqlErrorCollectionAddDelegate(sqlErrorCollection, sqlError);

        SqlException sqlException = SqlExceptionConstructorDelegate(errorMessage, sqlErrorCollection, null, Guid.NewGuid());

        return sqlException;
    }

    public static SqlException CreateTransientException()
        => Create(10060);

    public static SqlException CreateNonTransientException()
        => Create(50404);

    private static Func<SqlErrorCollection> CreateSqlCollectionFactory()
        => Expression.Lambda<Func<SqlErrorCollection>>(
            Expression.New(
                typeof(SqlErrorCollection).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null))).Compile();

    private static Action<SqlErrorCollection, SqlError> CreateAddErrorMethod()
    {
        // (sqlErrorCollection, sqlError) => sqlErrorCollection.Add(SqlErrorCollection.Add(sqlError));
        MethodInfo methodInfo = typeof(SqlErrorCollection).GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new Type[] { typeof(SqlError) },
            modifiers: null);

        ParameterExpression sqlErrorCollection = Expression.Parameter(typeof(SqlErrorCollection), "sqlErrorCollection");
        ParameterExpression sqlError = Expression.Parameter(typeof(SqlError), "sqlError");

        return Expression.Lambda<Action<SqlErrorCollection, SqlError>>(
            Expression.Call(sqlErrorCollection, methodInfo, sqlError),
            new[] { sqlErrorCollection, sqlError }).Compile();
    }

    private static Func<int, SqlError> CreateSqlErrorFactory()
    {
        // infoNumber => new SqlError(infoNumber, (byte)0, (byte)0, null, null, null, 0, null);
        ConstructorInfo constructorInfo = typeof(SqlError).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception) },
            modifiers: null);

        ParameterExpression infoNumber = Expression.Parameter(typeof(int), "infoNumber");
        ConstantExpression zeroByte = Expression.Constant((byte)0, typeof(byte));
        ConstantExpression nullString = Expression.Constant(null, typeof(string));

        return Expression.Lambda<Func<int, SqlError>>(
            Expression.New(
                constructorInfo,
                infoNumber,
                zeroByte,
                zeroByte,
                nullString,
                nullString,
                nullString,
                Expression.Constant(0, typeof(int)),
                Expression.Constant(null, typeof(Exception))),
            infoNumber).Compile();
    }

    private static Func<string, SqlErrorCollection, Exception, Guid, SqlException> CreateSqlExceptionFactory()
    {
        // (message, errorCollection, innerException, clientConnectionId) => new SqlException(message, errorCollection, innerException, clientConnectionId);
        ConstructorInfo constructorInfo = typeof(SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
            modifiers: null);

        ParameterExpression message = Expression.Parameter(typeof(string), "message");
        ParameterExpression errorCollection = Expression.Parameter(typeof(SqlErrorCollection), "errorCollection");
        ParameterExpression innerException = Expression.Parameter(typeof(Exception), "innerException");
        ParameterExpression clientConnectionId = Expression.Parameter(typeof(Guid), "clientConnectionId");

        return Expression.Lambda<Func<string, SqlErrorCollection, Exception, Guid, SqlException>>(
            Expression.New(constructorInfo, message, errorCollection, innerException, clientConnectionId),
            message,
            errorCollection,
            innerException,
            clientConnectionId).Compile();
    }
}
