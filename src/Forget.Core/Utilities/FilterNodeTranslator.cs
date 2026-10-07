using Dapper;
using Forget.Core.Abstractions.Models;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using System.Collections;
using System.Collections.Immutable;


namespace Forget.Core.Utilities
{
    internal sealed class FilterNodeTranslator<TEntity> where TEntity : class
    {
        private readonly ImmutableDictionary<string, string> _columnNamesByPropertyName;
        private readonly SqlTranslationContext _ctx;

        private FilterNodeTranslator(SqlTranslationContext ctx)
        {
            _columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            _ctx = ctx;
        }

        public static (string, DynamicParameters?) Translate(ISqlDialectStrategy sqlDialectStrategy, IFilterNode<TEntity> node, DynamicParameters? parameters = null)
        {
            SqlTranslationContext ctx = new(sqlDialectStrategy, parameters);
            FilterNodeTranslator<TEntity> translator = new(ctx);

            translator.TranslateNode(node);

            return (ctx.SqlBuffer.ToString(), ctx.Parameters);
        }

        private void TranslateNode(IFilterNode<TEntity> node)
        {
            switch (node)
            {
                case FilterDescriptor<TEntity> f:
                    TranslateDescriptor(f);
                    break;
                case FilterGroup<TEntity> g:
                    TranslateGroup(g);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported filter node '{node.GetType()}'.");
            }
        }

        private void TranslateDescriptor(FilterDescriptor<TEntity> f)
        {
            bool parenthesize = true;
            string left = _ctx.SqlDialectStrategy.RenderIdentifier(_columnNamesByPropertyName[f.PropertyName]);
            string sql;

            if (f.Value is null)
            {
                sql = f.ComparisonOperator switch
                {
                    ComparisonOperator.Equal => _ctx.SqlDialectStrategy.IsNull(left),
                    ComparisonOperator.NotEqual => _ctx.SqlDialectStrategy.IsNotNull(left),
                    _ => "1 = 0"
                };
            }
            else if (f.ComparisonOperator is ComparisonOperator.In && f.Value is IEnumerable enumerable && f.Value is not string)
            {
                List<object> values = enumerable is ICollection collection ? new(collection.Count) : [];
                bool hasNull = false;

                foreach (object? item in enumerable)
                {
                    if (item is null) hasNull = true;
                    else values.Add(item);
                }

                if (values.Count == 0 && !hasNull)
                {
                    sql = "1 = 0";
                }
                else
                {
                    List<string> parts = [];
                    int conditionCount = 0;

                    if (hasNull)
                    {
                        parts.Add($"({_ctx.SqlDialectStrategy.IsNull(left)})");
                        conditionCount++;
                    }

                    if (values.Count > 0)
                    {
                        Type valueType = values[0].GetType();

                        if (values.Any(x => x.GetType() != valueType))
                        {
                            string p = _ctx.AddParameter(values.ToArray());
                            parts.Add($"({_ctx.SqlDialectStrategy.In(left, p, true)})");
                        }
                        else
                        {
                            bool isEnum = valueType.IsEnum;
                            Type type = isEnum ? Enum.GetUnderlyingType(valueType) : valueType;
                            Array array = Array.CreateInstance(type, values.Count);

                            for (int i = 0; i < values.Count; i++)
                                array.SetValue(isEnum ? Convert.ChangeType(values[i], type) : values[i], i);

                            string p = _ctx.AddParameter(array);
                            parts.Add($"({_ctx.SqlDialectStrategy.In(left, p, false)})");
                        }

                        conditionCount++;
                    }

                    if (conditionCount > 1)
                        sql = $"({string.Join(" OR ", parts)})";
                    else
                        sql = string.Join(" OR ", parts);

                    parenthesize = false;
                }
            }
            else if ((f.ComparisonOperator is ComparisonOperator.Contains or ComparisonOperator.StartsWith or ComparisonOperator.EndsWith) && f.Value is string value)
            {
                string right = _ctx.SqlDialectStrategy.EscapeLike(value);
                right = _ctx.AddParameter(right);

                if (f.IgnoreCase)
                {
                    left = _ctx.SqlDialectStrategy.ToLower(left);
                    right = _ctx.SqlDialectStrategy.ToLower(right);
                }

                right = f.ComparisonOperator switch
                {
                    ComparisonOperator.Contains => _ctx.SqlDialectStrategy.Concat("'%'", right, "'%'"),
                    ComparisonOperator.StartsWith => _ctx.SqlDialectStrategy.Concat(right, "'%'"),
                    ComparisonOperator.EndsWith => _ctx.SqlDialectStrategy.Concat("'%'", right),

                    _ => throw new NotSupportedException($"Operator '{f.ComparisonOperator}' cannot be applied to property '{f.PropertyName}' and value '{f.Value}'.")
                };

                sql = _ctx.SqlDialectStrategy.Like(left, right);
            }
            else
            {
                string right = _ctx.AddParameter(f.Value);

                if (f.IgnoreCase && f.Value is string)
                {
                    left = _ctx.SqlDialectStrategy.ToLower(left);
                    right = _ctx.SqlDialectStrategy.ToLower(right);
                }

                sql = f.ComparisonOperator switch
                {
                    ComparisonOperator.Equal => $"{left} = {right}",
                    ComparisonOperator.NotEqual => $"{left} <> {right}",
                    ComparisonOperator.GreaterThan => $"{left} > {right}",
                    ComparisonOperator.GreaterThanOrEqual => $"{left} >= {right}",
                    ComparisonOperator.LessThan => $"{left} < {right}",
                    ComparisonOperator.LessThanOrEqual => $"{left} <= {right}",

                    _ => throw new NotSupportedException($"Operator '{f.ComparisonOperator}' cannot be applied to property '{f.PropertyName}' and value '{f.Value}'.")
                };
            }

            if (parenthesize)
                sql = $"({sql})";

            if (f.Not)
                sql = $"NOT {sql}";

            _ctx.SqlBuffer.Append(sql);
        }

        private void TranslateGroup(FilterGroup<TEntity> g)
        {
            if (g.FilterNodes.Count == 0)
            {
                _ctx.SqlBuffer.Append("(1 = 1)");
                return;
            }

            string logical = g.LogicalOperator == LogicalOperator.AndAlso ? "AND" : "OR";

            _ctx.Push();
            _ctx.SqlBuffer.Append('(');

            for (int i = 0; i < g.FilterNodes.Count; i++)
            {
                TranslateNode(g.FilterNodes[i]);
                if (i < g.FilterNodes.Count - 1)
                    _ctx.SqlBuffer.Append($" {logical} ");
            }

            _ctx.SqlBuffer.Append(')');
            string sql = _ctx.Pop();

            if (g.Not)
                sql = $"NOT {sql}";

            _ctx.SqlBuffer.Append(sql);
        }
    }
}
