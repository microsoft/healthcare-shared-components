// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests;

public class IdentifierTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("\t ")]
    [InlineData("$")]
    [InlineData("foo!bar")]
    [InlineData("@variable")]
    [InlineData("@@tempTable")]
    [InlineData("[Missing Bracket")]
    [InlineData("\"Missing Quote")]
    [InlineData("[Unescaped]Delimiter]")]
    [InlineData("\"Unescaped\"Delimiter\"")]
    [InlineData("[]")]
    [InlineData("\"\"")]
    [InlineData("foo DROP DATABASE Production --")] // SQL Injection
    [InlineData("𐊗𐊕𐊐𐊎𐊆𐊍𐊆")] // Lycian (SMP Unicode Characters)
    [InlineData("😀")] // Emoticons
    [InlineData("ROWCOUNT")] // Reserved
    public void GivenInvalidDatabaseName_WhenChecked_ReturnFalse(string databaseName)
    {
        Assert.False(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered invalid");
    }

    [Theory]
    [InlineData("SomethingNormal")]
    [InlineData("_")]
    [InlineData("#")]
    [InlineData("D_D@7ab$e#")]
    [InlineData("#_D@7ab$e#")]
    [InlineData("__D@7ab$e#")]
    public void GivenValidRegularDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.True(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }

    [Theory]
    [InlineData("[SomethingNormal]")]
    [InlineData("[foo bar!]")]
    [InlineData("[Escaped]]Delimiter]")]
    [InlineData("[]]]")]
    [InlineData("[\"]")]
    [InlineData("[𐊗𐊕𐊐𐊎𐊆𐊍𐊆]")] // Lycian (SMP Unicode Characters)
    [InlineData("[PROCEDURE]")] // Reserved
    public void GivenValidBracketDelimitedDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.True(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }

    [Theory]
    [InlineData("\"SomethingNormal\"")]
    [InlineData("\"foo bar!\"")]
    [InlineData("\"Escaped\"\"Delimiter]\"")]
    [InlineData("\"\"\"\"")]
    [InlineData("\"]\"")]
    [InlineData("\"𐊗𐊕𐊐𐊎𐊆𐊍𐊆\"")] // Lycian (SMP Unicode Characters)
    [InlineData("\"PROCEDURE\"")] // Reserved
    public void GivenValidQuoteDelimitedDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.True(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }
}
