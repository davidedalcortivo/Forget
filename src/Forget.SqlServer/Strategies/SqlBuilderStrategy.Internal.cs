using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class SqlBuilderStrategy : BaseSqlBuilderStrategy<SqlDialectStrategy>
    {
        private void AppendUpdateRange<TEntity>(StringBuilder sqlBuffer, IReadOnlyList<PropertyInfo> updateProperties, string sourceTable, string targetTable, string? clause) where TEntity : class
        {
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;

            sqlBuffer.Append("UPDATE ");
            sqlBuffer.AppendLine(targetTable);
            sqlBuffer.Append("SET");
            sqlBuffer.AppendSetColumns<TEntity>(SqlDialectStrategy, updateProperties, sourceTable, null, "    ");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, string.Empty, targetTable);
            sqlBuffer.AppendLine();

            if (clause is null)
                sqlBuffer.AppendLine("    WITH (UPDLOCK, HOLDLOCK)");

            sqlBuffer.AppendLine("INNER JOIN (");
            sqlBuffer.AppendLine("    VALUES");
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(") AS ");
            sqlBuffer.Append(sourceTable);
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, properties, null, string.Empty, false, true);
            sqlBuffer.Append(')');
            sqlBuffer.AppendOnClause(clause, string.Empty);
        }
    }
}
