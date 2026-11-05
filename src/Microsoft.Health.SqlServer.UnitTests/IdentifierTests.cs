// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests;

[TestClass]
public class IdentifierTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("\t ")]
    [DataRow("$")]
    [DataRow("foo!bar")]
    [DataRow("@variable")]
    [DataRow("@@tempTable")]
    [DataRow("[Missing Bracket")]
    [DataRow("\"Missing Quote")]
    [DataRow("[Unescaped]Delimiter]")]
    [DataRow("\"Unescaped\"Delimiter\"")]
    [DataRow("[]")]
    [DataRow("\"\"")]
    [DataRow("foo DROP DATABASE Production --")] // SQL Injection
    [DataRow("𐊗𐊕𐊐𐊎𐊆𐊍𐊆")] // Lycian (SMP Unicode Characters)
    [DataRow("😀")] // Emoticons
    [DataRow("ROWCOUNT")] // Reserved
    public void GivenInvalidDatabaseName_WhenChecked_ReturnFalse(string databaseName)
    {
        Assert.IsFalse(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered invalid");
    }

    [TestMethod]
    [DataRow("SomethingNormal")]
    [DataRow("_")]
    [DataRow("#")]
    [DataRow("D_D@7ab$e#")]
    [DataRow("#_D@7ab$e#")]
    [DataRow("__D@7ab$e#")]
    public void GivenValidRegularDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.IsTrue(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }

    [TestMethod]
    [DataRow("[SomethingNormal]")]
    [DataRow("[foo bar!]")]
    [DataRow("[Escaped]]Delimiter]")]
    [DataRow("[]]]")]
    [DataRow("[\"]")]
    [DataRow("[𐊗𐊕𐊐𐊎𐊆𐊍𐊆]")] // Lycian (SMP Unicode Characters)
    [DataRow("[PROCEDURE]")] // Reserved
    public void GivenValidBracketDelimitedDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.IsTrue(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }

    [TestMethod]
    [DataRow("\"SomethingNormal\"")]
    [DataRow("\"foo bar!\"")]
    [DataRow("\"Escaped\"\"Delimiter]\"")]
    [DataRow("\"\"\"\"")]
    [DataRow("\"]\"")]
    [DataRow("\"𐊗𐊕𐊐𐊎𐊆𐊍𐊆\"")] // Lycian (SMP Unicode Characters)
    [DataRow("\"PROCEDURE\"")] // Reserved
    public void GivenValidQuoteDelimitedDatabaseName_WhenChecked_ReturnTrue(string databaseName)
    {
        Assert.IsTrue(Identifier.IsValidDatabase(databaseName), $"'{databaseName}' should be considered valid");
    }

    [TestMethod]
    [DataRow("SomethingNormal", "[SomethingNormal]")]
    [DataRow("foo-bar baz", "[foo-bar baz]")]
    [DataRow("Escaped]Delimiter", "[Escaped]]Delimiter]")]
    [DataRow("]]", "[]]]]]")]
    [DataRow("\"Quoted\"", "[\"Quoted\"]")]
    [DataRow("foo] DROP DATABASE Production --", "[foo]] DROP DATABASE Production --]")] // SQL Injection
    public void GivenRawName_WhenDelimited_ThenReturnValidBracketDelimitedName(string name, string expected)
    {
        string actual = Identifier.Delimit(name);

        Assert.AreEqual(expected, actual);
        Assert.IsTrue(Identifier.IsValidDatabase(actual), $"'{actual}' should be considered valid");
    }
}
