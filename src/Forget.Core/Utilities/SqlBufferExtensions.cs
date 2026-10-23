using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;


namespace Forget.Core.Utilities
{
    internal static class SqlBufferExtensions
    {
        public static StringBuilder AppendSeparator(this StringBuilder sqlBuffer, int index, int count, bool inLine)
        {
            if (index < count - 1)
            {
                if (inLine)
                    sqlBuffer.Append(", ");
                else
                    sqlBuffer.AppendLine(",");
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendColumns<TEntity>(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, IReadOnlyList<PropertyInfo> properties, string? table, string indentation, bool useAlias, bool inLine) where TEntity : class
        {
            if (properties.Count == 0)
                return sqlBuffer;

            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            if (!inLine)
                sqlBuffer.AppendLine();

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo property = properties[i];
                string columnName = columnNamesByPropertyName[property.Name];

                sqlBuffer.Append(indentation);

                if (table is not null)
                {
                    sqlBuffer.Append(table);
                    sqlBuffer.Append('.');
                }

                sqlBuffer.Append(sqlDialectStrategy.RenderIdentifier(columnName));

                if (useAlias && columnName != property.Name)
                {
                    sqlBuffer.Append(" AS ");
                    sqlBuffer.Append(sqlDialectStrategy.RenderIdentifier(property.Name));
                }

                sqlBuffer.AppendSeparator(i, properties.Count, inLine);
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendSetColumns<TEntity>(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, IReadOnlyList<PropertyInfo> properties, string? sourceTable, string? targetTable, string indentation) where TEntity : class
        {
            if (properties.Count == 0)
                return sqlBuffer;

            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            sqlBuffer.AppendLine();

            for (int i = 0; i < properties.Count; i++)
            {
                string column = sqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[properties[i].Name]);
                sqlBuffer.Append(indentation);

                if (targetTable is not null)
                {
                    sqlBuffer.Append(targetTable);
                    sqlBuffer.Append('.');
                }

                sqlBuffer.Append(column);
                sqlBuffer.Append(" = ");

                if (sourceTable is not null)
                {
                    sqlBuffer.Append(sourceTable);
                    sqlBuffer.Append('.');
                }

                sqlBuffer.Append(column);
                sqlBuffer.AppendSeparator(i, properties.Count, false);
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendFromTable<TEntity>(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, string indentation, string? alias) where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string? schemaName = EntityInfoCache<TEntity>.SchemaName;

            sqlBuffer.AppendLine();
            sqlBuffer.Append(indentation);
            sqlBuffer.AppendLine("FROM");
            sqlBuffer.Append(indentation);
            sqlBuffer.Append("    ");
            sqlBuffer.Append(sqlDialectStrategy.RenderTable(schemaName, tableName));

            if (alias is not null)
            {
                sqlBuffer.Append(" AS ");
                sqlBuffer.Append(alias);
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendWhereClause(this StringBuilder sqlBuffer, string? clause, string indentation)
        {
            if (clause is not null)
            {
                sqlBuffer.AppendLine();
                sqlBuffer.Append(indentation);
                sqlBuffer.AppendLine("WHERE");
                sqlBuffer.Append(indentation);
                sqlBuffer.Append("    ");
                sqlBuffer.Append(clause);
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendOnClause(this StringBuilder sqlBuffer, string? clause, string indentation)
        {
            if (clause is not null)
            {
                sqlBuffer.AppendLine();
                sqlBuffer.Append(indentation);
                sqlBuffer.AppendLine("ON");
                sqlBuffer.Append(indentation);
                sqlBuffer.Append("    ");
                sqlBuffer.Append(clause);
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendNullSafeEquals(this StringBuilder sqlBuffer, string left, string right, int index, string indentation)
        {
            if (index > 0)
            {
                sqlBuffer.AppendLine();
                sqlBuffer.Append(indentation);
                sqlBuffer.AppendLine("AND");
            }

            sqlBuffer.Append(indentation);
            sqlBuffer.AppendLine("(");
            sqlBuffer.Append(indentation);
            sqlBuffer.Append("    ");
            sqlBuffer.Append(left);
            sqlBuffer.Append(" = ");
            sqlBuffer.AppendLine(right);
            sqlBuffer.Append(indentation);
            sqlBuffer.Append("    OR (");
            sqlBuffer.Append(left);
            sqlBuffer.Append(" IS NULL AND ");
            sqlBuffer.Append(right);
            sqlBuffer.AppendLine(" IS NULL)");
            sqlBuffer.Append(indentation);
            sqlBuffer.Append(')');

            return sqlBuffer;
        }

        public static StringBuilder AppendSort<TEntity>(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, bool forceSorting) where TEntity : class
        {
            List<SortDescriptor<TEntity>> sortDescriptorList = sortDescriptors is null ? [] : sortDescriptors.AsList();

            if (forceSorting && sortDescriptorList.Count == 0)
                sortDescriptorList.Add(new(EntityInfoCache<TEntity>.IdProperty.Name));

            if (sortDescriptorList.Count > 0)
            {
                ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

                sqlBuffer.AppendLine();
                sqlBuffer.AppendLine("ORDER BY");

                for (int i = 0; i < sortDescriptorList.Count; i++)
                {
                    SortDescriptor<TEntity> sortDescriptor = sortDescriptorList[i];

                    string columnName = columnNamesByPropertyName[sortDescriptor.PropertyName];
                    string sortDirection = sortDescriptor.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";

                    sqlBuffer.Append("    ");
                    sqlBuffer.Append(sqlDialectStrategy.RenderIdentifier(columnName));
                    sqlBuffer.Append(' ');
                    sqlBuffer.Append(sortDirection);
                    sqlBuffer.AppendSeparator(i, sortDescriptorList.Count, false);
                }
            }

            return sqlBuffer;
        }

        public static StringBuilder AppendAndBindParameter(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, DynamicParameters parameters, string name, object? value)
        {
            sqlBuffer.Append(sqlDialectStrategy.RenderParameter(name));
            parameters.Add(name, value);

            return sqlBuffer;
        }

        public static StringBuilder AppendRowParameters<TEntity>(this StringBuilder sqlBuffer, ISqlDialectStrategy sqlDialectStrategy, DynamicParameters parameters, ImmutableArray<PropertyInfo> properties, TEntity entity, int row) where TEntity : class
        {
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;

            for (int i = 0; i < properties.Length; i++)
            {
                string propertyName = properties[i].Name;
                object? parameterValue = propertyGettersByPropertyName[propertyName](entity);

                sqlBuffer.AppendAndBindParameter(sqlDialectStrategy, parameters, $"{propertyName}_{row}", parameterValue);
                sqlBuffer.AppendSeparator(i, properties.Length, true);
            }

            return sqlBuffer;
        }
    }
}
