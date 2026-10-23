using Forget.Core.Utilities;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Forget quotes every table, schema and column name, doubling the closing quote character, so any name the database
    /// accepts can be used. Two kinds of name are refused, because something rewrites the SQL text before the database
    /// reads it, without looking at quotes: a curly brace, which marks the places to fill in Forget's SQL templates (and
    /// Dapper replaces <c>{=name}</c> with a value), and a piece of text Dapper takes for a parameter, on every provider:
    /// <c>?</c>, <c>@</c>, <c>:</c> or <c>$</c> followed by the name of a list Forget binds (<c>p_1_</c>,
    /// <c>IdArray_0_</c>), which Dapper expands into one parameter per element, or a name between two question marks,
    /// which Dapper passes by position.
    /// </summary>
    public class IdentifierHelperTests
    {
        [Theory]
        [InlineData("Order Details")]
        [InlineData("Città")]
        [InlineData("ORDER#")]
        [InlineData("Cost$")]
        [InlineData("V$SESSION")]
        [InlineData("C##APP")]
        [InlineData("my-app")]
        [InlineData("2nd-Value")]
        [InlineData("_Flag")]
        [InlineData("__EFMigrationsHistory")]
        [InlineData("Back\\slash")]
        [InlineData("Dot.Name")]
        [InlineData("Quote\"Name")]
        [InlineData("Bracket]Name")]
        [InlineData("Tick`Name")]
        [InlineData("Email@Work")]
        [InlineData("a:b")]
        [InlineData("What?")]
        [InlineData("temp_1")]
        [InlineData("backup_2024")]
        [InlineData("a@p_1_b")]
        [InlineData("a?two words?")]
        public void AName_TheDatabaseAccepts_IsAccepted(string name)
        {
            IdentifierHelper.EnsureValid("column name", name);
        }

        [Theory]
        [InlineData("a{b")]
        [InlineData("a}b")]
        [InlineData("{=p_0}")]
        public void AName_WithACurlyBrace_IsRefused(string name)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => IdentifierHelper.EnsureValid("column name", name));

            Assert.Contains("curly brace", ex.Message);
        }

        [Theory]
        [InlineData("c@p_1_", "@p_1_")]
        [InlineData("c:P_1_", ":P_1_")]
        [InlineData("c?p_1_", "?p_1_")]
        [InlineData("c$p_12_", "$p_12_")]
        [InlineData("x@IdArray_0_", "@IdArray_0_")]
        [InlineData("x:CodeArray_3_ total", ":CodeArray_3_")]
        [InlineData("a?Name?b", "?Name?")]
        [InlineData("a?p_0?", "?p_0?")]
        public void AName_ContainingWhatDapperTakesForAParameter_IsRefused(string name, string token)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => IdentifierHelper.EnsureValid("column name", name));

            Assert.Contains($"'{token}'", ex.Message);
        }
    }
}
