using Forget.Core.Abstractions.Strategies;
using Forget.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Core
{
    /// <summary>
    /// An entity with a schema in <c>[Table]</c> is qualified with that schema in every statement. An entity without
    /// one is left unqualified, so the database resolves the table from the session, as it resolves hand-written SQL: the
    /// database of the connection on MySQL, the current schema on Oracle, the search_path on PostgreSQL, the user's
    /// default schema on SQL Server. Forget used to qualify it with a schema of its own (the database or user of the
    /// first connection, public, dbo), which ignored the session.
    /// </summary>
    public class TableNameTests
    {
        private sealed class NoSchemaRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Table("SchemaRow", Schema = "sales")]
        private sealed class SchemaRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private static ISqlBuilderStrategy Builder(string providerName)
        {
            return providerName switch
            {
                "MySql" => Forget.MySql.Strategies.SqlBuilderStrategy.Instance,
                "Oracle" => Forget.Oracle.Strategies.SqlBuilderStrategy.Instance,
                "PostgreSql" => Forget.PostgreSql.Strategies.SqlBuilderStrategy.Instance,
                "SqlServer" => Forget.SqlServer.Strategies.SqlBuilderStrategy.Instance,
                _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
            };
        }

        // Every template the builder makes for the entity, as text: a template rendered without values keeps its placeholders.
        private static List<string> Templates<TEntity>(ISqlBuilderStrategy builder) where TEntity : class
        {
            return [.. typeof(ISqlBuilderStrategy).GetMethods()
                .Where(x => x.Name.EndsWith("SqlBuilder") && x.IsGenericMethodDefinition)
                .Select(x => ((SqlTemplate)x.MakeGenericMethod(typeof(TEntity)).Invoke(builder, null)!).Render())];
        }
        
        // Only the provider's name goes in the theory data: Test Explorer lists a row per provider only for values xUnit can
        // serialize, which a builder is not.
        public static TheoryData<string> Providers()
        {
            return ["MySql", "Oracle", "PostgreSql", "SqlServer"];
        }

        [Theory]
        [MemberData(nameof(Providers))]
        public void AnEntityWithoutASchema_IsNeverQualified(string providerName)
        {
            ISqlBuilderStrategy builder = Builder(providerName);
            string table = builder.SqlDialectStrategy.RenderIdentifier(nameof(NoSchemaRow));

            List<string> templates = Templates<NoSchemaRow>(builder);

            Assert.All(templates, x => Assert.DoesNotContain($".{table}", x));
            Assert.True(templates.Count(x => x.Contains(table)) >= 10, "most statements name the table");
        }

        [Theory]
        [MemberData(nameof(Providers))]
        public void AnEntityWithASchema_IsAlwaysQualifiedWithIt(string providerName)
        {
            ISqlBuilderStrategy builder = Builder(providerName);
            string table = builder.SqlDialectStrategy.RenderIdentifier("SchemaRow");
            string qualified = $"{builder.SqlDialectStrategy.RenderIdentifier("sales")}.{table}";

            List<string> templates = Templates<SchemaRow>(builder);

            Assert.All(templates, x => Assert.DoesNotContain(table, x.Replace(qualified, string.Empty)));
            Assert.True(templates.Count(x => x.Contains(qualified)) >= 10, "most statements name the table");
        }
    }
}
