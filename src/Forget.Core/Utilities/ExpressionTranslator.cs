using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.Core.Utilities
{
    internal sealed class ExpressionTranslator<TEntity> : ExpressionVisitor where TEntity : class
    {
        private readonly ImmutableDictionary<string, string> _columnNamesByPropertyName;
        private readonly SqlTranslationContext _ctx;

        private ExpressionTranslator(SqlTranslationContext ctx)
        {
            _columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            _ctx = ctx;
        }

        public static (string, DynamicParameters?) Translate(ISqlDialectStrategy sqlDialectStrategy, Expression<Func<TEntity, bool>> expression, DynamicParameters? parameters = null)
        {
            SqlTranslationContext ctx = new(sqlDialectStrategy, parameters);
            ExpressionTranslator<TEntity> translator = new(ctx);

            translator.Visit(expression);

            return (ctx.SqlBuffer.ToString(), ctx.Parameters);
        }

        private string ExtractSql(Expression expr, bool lower = false)
        {
            _ctx.Push();
            Visit(expr);
            string sql = _ctx.Pop();
            return lower ? _ctx.SqlDialectStrategy.ToLower(sql) : sql;
        }

        private static bool TryEval(Expression expr, out object? value)
        {
            return TryEval(expr, out value, out _);
        }

        private static bool TryEval(Expression expr, out object? value, out Exception? failure)
        {
            failure = null;

            switch (expr)
            {
                case ConstantExpression ce:
                    value = ce.Value;
                    return true;

                case MemberExpression me:
                    return MemberEvaluatorCache.TryEvaluate(me, out value, out failure);
            }

            try
            {
                value = Expression.Lambda(expr).Compile().DynamicInvoke();
                return true;
            }
            catch (Exception ex)
            {
                value = null;
                failure = ex is TargetInvocationException { InnerException: not null } ? ex.InnerException : ex;
                return false;
            }
        }

        private string RenderColumn(MemberExpression member)
        {
            if (!_columnNamesByPropertyName.TryGetValue(member.Member.Name, out string? columnName))
                throw new NotSupportedException($"The property '{member.Member.Name}' of entity '{typeof(TEntity).Name}' is not mapped to a column, so it cannot be used in a predicate.");

            return _ctx.SqlDialectStrategy.RenderIdentifier(columnName);
        }

        private static bool IsNull(Expression expr)
        {
            return expr is ConstantExpression { Value: null } || TryEval(expr, out object? v) && v is null;
        }

        private static bool IsStringEquals(MethodCallExpression m)
        {
            return m.Method.DeclaringType == typeof(string) && m.Method.Name == nameof(string.Equals);
        }

        private static bool IsStringCompare(MethodCallExpression m)
        {
            return m.Method.DeclaringType == typeof(string) && m.Method.Name == nameof(string.Compare);
        }

        private bool ResolveIgnoreCase(MethodCallExpression m)
        {
            if (m.Method.Name == nameof(string.Compare) &&
                m.Arguments.Count == 3 &&
                m.Arguments[2] is ConstantExpression ceBool &&
                ceBool.Value is bool b)
                return b;

            foreach (Expression arg in m.Arguments)
            {
                if (arg is ConstantExpression ce &&
                    ce.Value is StringComparison sc &&
                    sc.ToString().EndsWith("IgnoreCase", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _ctx.SqlBuffer.Append(_ctx.AddParameter(node.Value));
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                _ctx.SqlBuffer.Append(RenderColumn(node));
                return node;
            }

            if (!TryEval(node, out object? value, out Exception? failure))
                throw new NotSupportedException($"Unable to evaluate expression '{node}': it is not a mapped column of the entity, and it cannot be computed without the entity either.", failure);

            _ctx.SqlBuffer.Append(_ctx.AddParameter(value));
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not && 
                node.Operand is MemberExpression member &&
                member.Expression is ParameterExpression &&
                member.Type == typeof(bool))
            {
                string col = RenderColumn(member);

                _ctx.SqlBuffer.Append("NOT (");
                _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.IsTrue(col));
                _ctx.SqlBuffer.Append(')');

                return node;
            }

            if (node.NodeType == ExpressionType.Not)
            {
                _ctx.SqlBuffer.Append("NOT ");

                if (node.Operand is UnaryExpression { NodeType: ExpressionType.Not })
                {
                    _ctx.SqlBuffer.Append('(');
                    Visit(node.Operand);
                    _ctx.SqlBuffer.Append(')');
                }
                else
                {
                    Visit(node.Operand);
                }

                return node;
            }

            if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                return Visit(node.Operand);

            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left is MethodCallExpression mLeft && IsStringCompare(mLeft))
                return VisitCompareBinary(node, mLeft);

            if (node.Right is MethodCallExpression mRight && IsStringCompare(mRight))
            {
                BinaryExpression flip = Expression.MakeBinary(
                    Flip(node.NodeType),
                    node.Right,
                    node.Left
                );

                return VisitBinary(flip);
            }

            if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                bool leftNull = IsNull(node.Left);
                bool rightNull = IsNull(node.Right);

                if (leftNull ^ rightNull)
                {
                    Expression col = leftNull ? node.Right : node.Left;
                    string sql = ExtractSql(col);

                    _ctx.SqlBuffer.Append('(');
                    _ctx.SqlBuffer.Append(
                        node.NodeType == ExpressionType.Equal
                            ? _ctx.SqlDialectStrategy.IsNull(sql)
                            : _ctx.SqlDialectStrategy.IsNotNull(sql)
                    );
                    _ctx.SqlBuffer.Append(')');

                    return node;
                }
            }

            _ctx.SqlBuffer.Append('(');

            if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
                VisitPredicate(node.Left);
            else
                Visit(node.Left);

            string op = node.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Unsupported operator '{node.NodeType}'.")
            };

            _ctx.SqlBuffer.Append($" {op} ");

            if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
                VisitPredicate(node.Right);
            else
                Visit(node.Right);

            _ctx.SqlBuffer.Append(')');
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(ToString) && node.Object is not null && node.Arguments.Count == 0)
                return VisitToString(node);

            if (node.Method.DeclaringType == typeof(string) && node.Object is not null && node.Arguments.Count == 0)
            {
                if (node.Method.Name == nameof(string.ToLower) || node.Method.Name == nameof(string.ToLowerInvariant))
                    return VisitToLower(node);

                if (node.Method.Name == nameof(string.ToUpper) || node.Method.Name == nameof(string.ToUpperInvariant))
                    return VisitToUpper(node);
            }

            if (node.Method.DeclaringType == typeof(string))
                return VisitStringMethod(node);

            if (node.Method.Name == nameof(Enumerable.Contains))
            {
                Type sourceType = node.Object?.Type ?? node.Arguments[0].Type;

                if (typeof(IEnumerable).IsAssignableFrom(sourceType))
                    return VisitCollectionContains(node);
            }

            throw new NotSupportedException($"Unsupported method '{node.Method.Name}'.");
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Body is MemberExpression member &&
                member.Expression is ParameterExpression &&
                member.Type == typeof(bool))
            {
                string col = RenderColumn(member);

                _ctx.SqlBuffer.Append('(');
                _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.IsTrue(col));
                _ctx.SqlBuffer.Append(')');

                return node;
            }

            return base.VisitLambda(node);
        }

        private static ExpressionType Flip(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.GreaterThan => ExpressionType.LessThan,
                ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
                ExpressionType.LessThan => ExpressionType.GreaterThan,
                ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
                _ => type
            };
        }

        private void VisitPredicate(Expression expr)
        {
            if (expr is MemberExpression member &&
                member.Expression is ParameterExpression &&
                member.Type == typeof(bool))
            {
                string col = RenderColumn(member);

                _ctx.SqlBuffer.Append('(');
                _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.IsTrue(col));
                _ctx.SqlBuffer.Append(')');

                return;
            }

            Visit(expr);
        }

        private MethodCallExpression VisitStringMethod(MethodCallExpression m)
        {
            if (IsStringEquals(m))
            {
                Expression a;
                Expression b;

                if (m.Object is not null)
                {
                    if (m.Arguments.Count is not (1 or 2))
                        throw new NotSupportedException($"Unsupported Equals overload '{m}'.");

                    a = m.Object;
                    b = m.Arguments[0];
                }
                else
                {
                    if (m.Arguments.Count is not (2 or 3))
                        throw new NotSupportedException($"Unsupported Equals overload '{m}'.");

                    a = m.Arguments[0];
                    b = m.Arguments[1];
                }

                bool ignoreCase = ResolveIgnoreCase(m);

                bool aNull = IsNull(a);
                bool bNull = IsNull(b);

                if (aNull && bNull)
                {
                    _ctx.SqlBuffer.Append("(1 = 1)");
                    return m;
                }

                string left = aNull ? string.Empty : ExtractSql(a, ignoreCase);
                string right = bNull ? string.Empty : ExtractSql(b, ignoreCase);

                if (aNull)
                {
                    _ctx.SqlBuffer.Append($"({_ctx.SqlDialectStrategy.IsNull(right)})");
                    return m;
                }

                if (bNull)
                {
                    _ctx.SqlBuffer.Append($"({_ctx.SqlDialectStrategy.IsNull(left)})");
                    return m;
                }

                _ctx.SqlBuffer.Append($"({left} = {right})");
                return m;
            }

            if (m.Method.Name is "Contains" or "StartsWith" or "EndsWith")
            {
                if (m.Arguments.Count is not (1 or 2))
                    throw new NotSupportedException($"Unsupported {m.Method.Name} overload '{m}'.");

                bool ignoreCase = ResolveIgnoreCase(m);

                if (!TryEval(m.Arguments[0], out object? raw, out Exception? failure))
                    throw new NotSupportedException($"The argument of '{m.Method.Name}' must be a value that can be evaluated without the entity, but '{m.Arguments[0]}' cannot.", failure);

                if (raw is null)
                {
                    _ctx.SqlBuffer.Append("(1 = 0)");
                    return m;
                }

                string val = _ctx.SqlDialectStrategy.EscapeLike(raw.ToString()!);
                string p = _ctx.AddParameter(val);

                string col = ExtractSql(m.Object!, ignoreCase);
                string pp = ignoreCase ? _ctx.SqlDialectStrategy.ToLower(p) : p;

                string pat = m.Method.Name switch
                {
                    "Contains" => _ctx.SqlDialectStrategy.Concat("'%'", pp, "'%'"),
                    "StartsWith" => _ctx.SqlDialectStrategy.Concat(pp, "'%'"),
                    "EndsWith" => _ctx.SqlDialectStrategy.Concat("'%'", pp),
                    _ => throw new NotSupportedException($"Unsupported method '{m.Method.Name}'.")
                };

                _ctx.SqlBuffer.Append($"({_ctx.SqlDialectStrategy.Like(col, pat)})");
                return m;
            }

            throw new NotSupportedException($"Unsupported string method '{m.Method.Name}'.");
        }

        private MethodCallExpression VisitToString(MethodCallExpression m)
        {
            string sql = ExtractSql(m.Object!);
            _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.CastAsString(sql));

            return m;
        }

        private MethodCallExpression VisitToLower(MethodCallExpression m)
        {
            string sql = ExtractSql(m.Object!);
            _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.ToLower(sql));

            return m;
        }

        private MethodCallExpression VisitToUpper(MethodCallExpression m)
        {
            string sql = ExtractSql(m.Object!);
            _ctx.SqlBuffer.Append(_ctx.SqlDialectStrategy.ToUpper(sql));

            return m;
        }

        private BinaryExpression VisitCompareBinary(BinaryExpression be, MethodCallExpression m)
        {
            if (be.Right is not ConstantExpression ce || (int?)ce.Value != 0)
                throw new NotSupportedException("string.Compare must be compared against 0.");

            bool ignoreCase = ResolveIgnoreCase(m);

            bool aNull = IsNull(m.Arguments[0]);
            bool bNull = IsNull(m.Arguments[1]);

            string a = ExtractSql(m.Arguments[0], ignoreCase);
            string b = ExtractSql(m.Arguments[1], ignoreCase);

            switch (be.NodeType)
            {
                case ExpressionType.Equal:
                    if (aNull && bNull)
                    {
                        _ctx.SqlBuffer.Append("(1 = 1)");
                        return be;
                    }

                    if (aNull)
                    {
                        _ctx.SqlBuffer.Append($"({_ctx.SqlDialectStrategy.IsNull(b)})");
                        return be;
                    }

                    if (bNull)
                    {
                        _ctx.SqlBuffer.Append($"({_ctx.SqlDialectStrategy.IsNull(a)})");
                        return be;
                    }

                    _ctx.SqlBuffer.Append($"({a} = {b})");
                    return be;

                case ExpressionType.LessThan:
                    _ctx.SqlBuffer.Append("(" +
                        $"(({_ctx.SqlDialectStrategy.IsNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)})) OR " +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)}) AND ({a} < {b}))" +
                        ")");
                    return be;

                case ExpressionType.GreaterThan:
                    _ctx.SqlBuffer.Append("(" +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNull(b)})) OR " +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)}) AND ({a} > {b}))" +
                        ")");
                    return be;

                case ExpressionType.LessThanOrEqual:
                    _ctx.SqlBuffer.Append("(" +
                        $"(({_ctx.SqlDialectStrategy.IsNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)})) OR " +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)}) AND ({a} <= {b}))" +
                        ")");
                    return be;

                case ExpressionType.GreaterThanOrEqual:
                    _ctx.SqlBuffer.Append("(" +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNull(b)})) OR " +
                        $"(({_ctx.SqlDialectStrategy.IsNotNull(a)}) AND ({_ctx.SqlDialectStrategy.IsNotNull(b)}) AND ({a} >= {b}))" +
                        ")");
                    return be;

                default:
                    throw new NotSupportedException($"Unsupported operator '{be.NodeType}'.");
            }
        }

        private MethodCallExpression VisitCollectionContains(MethodCallExpression m)
        {
            object? raw =
                m.Object is not null
                    ? Expression.Lambda(m.Object).Compile().DynamicInvoke()
                    : Expression.Lambda(m.Arguments[0]).Compile().DynamicInvoke();

            if (raw is null && typeof(IEnumerable).IsAssignableFrom(m.Object?.Type ?? m.Arguments[0].Type))
            {
                _ctx.SqlBuffer.Append("(1 = 0)");
                return m;
            }

            if (raw is not IEnumerable coll)
                throw new NotSupportedException("Contains requires an IEnumerable source.");

            List<object> values = [];
            bool hasNull = false;

            foreach (object? item in coll)
            {
                if (item is null) hasNull = true;
                else values.Add(item);
            }

            Expression element = m.Object is not null ? m.Arguments[0] : m.Arguments[1];
            string colSql = ExtractSql(element);

            if (values.Count == 0 && !hasNull)
            {
                _ctx.SqlBuffer.Append("(1 = 0)");
                return m;
            }

            List<string> parts = [];
            int conditionCount = 0;

            if (hasNull)
            {
                parts.Add($"({_ctx.SqlDialectStrategy.IsNull(colSql)})");
                conditionCount++;
            }

            if (values.Count > 0)
            {
                Type valueType = values[0].GetType();

                if (values.Any(x => x.GetType() != valueType))
                {
                    string p = _ctx.AddParameter(values.ToArray());
                    parts.Add($"({_ctx.SqlDialectStrategy.In(colSql, p, true)})");
                }
                else
                {
                    bool isEnum = valueType.IsEnum;
                    Type type = isEnum ? Enum.GetUnderlyingType(valueType) : valueType;
                    Array array = Array.CreateInstance(type, values.Count);

                    for (int i = 0; i < values.Count; i++)
                        array.SetValue(isEnum ? Convert.ChangeType(values[i], type) : values[i], i);

                    string p = _ctx.AddParameter(array);
                    parts.Add($"({_ctx.SqlDialectStrategy.In(colSql, p, false)})");
                }
                
                conditionCount++;
            }

            if (conditionCount > 1)
                _ctx.SqlBuffer.Append($"({string.Join(" OR ", parts)})");
            else
                _ctx.SqlBuffer.Append(string.Join(" OR ", parts));

            return m;
        }
    }
}
