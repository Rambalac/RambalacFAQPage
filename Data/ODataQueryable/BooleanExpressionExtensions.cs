// <copyright file="BooleanExpressionExtensions.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using global::ODataQuery.Strings;

    /// <summary>
    /// Extensions for TableQuery.
    /// </summary>
    public class BooleanExpressionExtensions
    {
        /// <summary>
        /// Add conditions for query. Convert properties marked as PartitionKey or RowKey as such key.
        /// </summary>
        /// <typeparam name="TEntity">Table entity.</typeparam>
        /// <param name="expression">Condition expression.</param>
        /// <returns>Updated query.</returns>
        public string MakeFilter<TEntity>(Expression<Func<TEntity, bool>> expression)
        {
            var visitor = new PropertyReplacer<TEntity>();
            var newFunc = visitor.VisitAndConvert(expression);
            return ConvertTop(newFunc.Body);
        }

        /// <summary>
        /// Combines the binary.
        /// </summary>
        /// <param name="op">The op.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="obj">The object.</param>
        /// <returns>Expression.</returns>
        protected virtual string CombineBinary(string op, BinaryExpression parameters, Parameter obj)
        {
            var left = ConvertTop(parameters.Left, obj);
            if (left.EndsWith(')'))
            {
                return $"{left} {op} ({ConvertTop(parameters.Right, obj)})";
            }

            return $"({left}) {op} ({ConvertTop(parameters.Right, obj)})";
        }

        /// <summary>
        /// Combines the not.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="obj">The object.</param>
        /// <returns>Expression.</returns>
        protected virtual string CombineNot(UnaryExpression parameter, Parameter obj)
        {
            return $"not ({ConvertTop(parameter.Operand, obj)})";
        }

        /// <summary>
        /// Converts the binary.
        /// </summary>
        /// <param name="op">The op.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// Expression.
        /// </returns>
        protected virtual string ConvertBinary(string op, BinaryExpression expression, Parameter obj)
        {
            var left = GetGettingProperty(expression.Left, out var enumType);
            if (left != null)
            {
                object value;
                if (obj == null)
                {
                    value = Expression.Lambda(expression.Right).Compile().DynamicInvoke();
                }
                else
                {
                    value = Expression.Lambda(expression.Right, obj.ParameterExpression)
                                      .Compile()
                                      .DynamicInvoke(obj.Value);
                }

                if (enumType != null)
                {
                    value = ConvertToEnumString(enumType, value);
                }

                return GenerateFilter(left, op, value);
            }

            var right = GetGettingProperty(expression.Right, out enumType);
            if (right != null)
            {
                object value;
                if (obj == null)
                {
                    value = Expression.Lambda(expression.Left).Compile().DynamicInvoke();
                }
                else
                {
                    value = Expression.Lambda(expression.Left, obj.ParameterExpression)
                                      .Compile()
                                      .DynamicInvoke(obj.Value);
                }

                if (enumType != null)
                {
                    value = ConvertToEnumString(enumType, value);
                }

                return GenerateFilter(right, InvertOp(op), value);
            }

            throw new InvalidOperationException(Errors.TableEntityRequired);
        }

        /// <summary>
        /// Converts the enumerable.
        /// </summary>
        /// <param name="isAny">if set to <c>true</c> [is any].</param>
        /// <param name="callExp">The call expression.</param>
        /// <returns>
        /// Expression.
        /// </returns>
        protected virtual string ConvertEnumerable(bool isAny, MethodCallExpression callExp)
        {
            var str = new StringBuilder();

            var leftProperty = GetGettingProperty(callExp.Arguments[0], out var enumType);
            if (leftProperty == null)
            {
                var connector = isAny ? "or" : "and";
                str.Append("(");
                var en = (IEnumerable)Expression.Lambda(callExp.Arguments[0]).Compile().DynamicInvoke();
                var subExp = (LambdaExpression)callExp.Arguments[1];

                var first = true;
                foreach (var obj in en)
                {
                    if (!first)
                    {
                        str.Append($" {connector} ");
                    }
                    else
                    {
                        first = false;
                    }

                    str.Append("(");
                    var parameter = new Parameter(subExp.Parameters[0], obj);
                    str.Append(ConvertTop(subExp.Body, parameter));
                    str.Append(")");
                }

                str.Append(")");
                return str.ToString();
            }

            // PublicRange/any(item: item eq 'ALL')
            str.Append(leftProperty);
            str.Append("/");
            str.Append(isAny ? "any" : "all");
            str.Append("(");
            if (callExp.Arguments.Count == 2)
            {
                str.Append("item: ");
                var lambda = (LambdaExpression)callExp.Arguments[1];
                str.Append(ConvertTop(lambda.Body));
            }

            str.Append(")");
            return str.ToString();
        }

        /// <summary>
        /// Converts the next.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="obj">The object.</param>
        /// <returns>Expression.</returns>
        protected virtual string ConvertNext(Expression expression, Parameter obj)
        {
            var op = expression.NodeType switch
            {
                ExpressionType.Equal => "eq",
                ExpressionType.GreaterThan => "gt",
                ExpressionType.GreaterThanOrEqual => "ge",
                ExpressionType.LessThan => "lt",
                ExpressionType.LessThanOrEqual => "le",
                ExpressionType.NotEqual => "ne",
                _ => throw new InvalidOperationException(
                         "Not supported second level expression: " + expression.NodeType)
            };

            return ConvertBinary(op, (BinaryExpression)expression, obj);
        }

        /// <summary>
        /// Converts to enum string.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="value">The value.</param>
        /// <returns>Expression.</returns>
        protected virtual string ConvertToEnumString(Type enumType, object value)
        {
            enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            var name = Enum.GetName(enumType, value);
            var info = enumType.GetMember(name).FirstOrDefault(m => m.DeclaringType == enumType);
            return info?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? name;
        }

        /// <summary>
        /// Converts the top.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// Expression.
        /// </returns>
        /// <exception cref="ArgumentException">Not Member access expression.</exception>
        /// <exception cref="InvalidOperationException">Operation is not supported: " + expression.NodeType.</exception>
        protected virtual string ConvertTop(Expression expression, Parameter obj = null)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return GetGettingProperty(expression, out _) ??
                           throw new ArgumentException(Errors.ExpressionMemberExpression);
                case ExpressionType.AndAlso:
                    return CombineBinary("and", (BinaryExpression)expression, obj);
                case ExpressionType.Not:
                    return CombineNot((UnaryExpression)expression, obj);
                case ExpressionType.OrElse:
                    return CombineBinary("or", (BinaryExpression)expression, obj);
                case ExpressionType.Coalesce:
                    return ConvertTop(((BinaryExpression)expression).Left, obj);
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return ConvertNext(expression, obj);
                case ExpressionType.Call:
                    var callExpression = (MethodCallExpression)expression;
                    if (callExpression.Method.DeclaringType == typeof(Enumerable))
                    {
                        switch (callExpression.Method.Name)
                        {
                            case "Any":
                                return ConvertEnumerable(true, callExpression);
                            case "All":
                                return ConvertEnumerable(false, callExpression);
                        }
                    }

                    goto default;
                default:
                    throw new InvalidOperationException("Operation is not supported: " + expression.NodeType);
            }
        }

        /// <summary>
        /// Generates the filter.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="value">The value.</param>
        /// <returns>Expression.</returns>
        /// <exception cref="ArgumentNullException">Null values comparison is not supported: {left}.</exception>
        protected virtual string GenerateFilter(string left, string op, object value)
        {
            return value switch
            {
                null => throw new ArgumentNullException($"Null values comparison is not supported: {left}"),
                string s => GenerateFilterCondition(left, op, s),
                bool b => GenerateFilterConditionForBool(left, op, b),
                DateTime d => GenerateFilterConditionForDate(left, op, d),
                DateTimeOffset d => GenerateFilterConditionForDate(left, op, d),
                double d => GenerateFilterConditionForDouble(left, op, d),
                float f => GenerateFilterConditionForDouble(left, op, f),
                byte i => GenerateFilterConditionForLong(left, op, i),
                short i => GenerateFilterConditionForLong(left, op, i),
                int i => GenerateFilterConditionForLong(left, op, i),
                long l => GenerateFilterConditionForLong(left, op, l),
                _ => throw new InvalidOperationException($"Type is not supported: {value.GetType()}")
            };
        }

        /// <summary>
        /// Generates the filter condition.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="s">The s.</param>
        /// <returns>Expression.</returns>
        protected virtual string GenerateFilterCondition(string left, string op, string s)
        {
            return $"{left} {op} '{s}'";
        }

        /// <summary>
        /// Generates the filter condition for bool.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="b">if set to <c>true</c> [b].</param>
        /// <returns>Expression.</returns>
        protected virtual string GenerateFilterConditionForBool(string left, string op, in bool b)
        {
            var bb = b ? "true" : "false";
            return $"{left} {op} {bb}";
        }

        /// <summary>
        /// Generates the filter condition for date.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="d">The d.</param>
        /// <returns>Expression.</returns>
        protected virtual string GenerateFilterConditionForDate(string left, string op, in DateTimeOffset d)
        {
            return $"{left} {op} '{d:O}'";
        }

        /// <summary>
        /// Generates the filter condition for double.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="d">The d.</param>
        /// <returns>Expression.</returns>
        protected virtual string GenerateFilterConditionForDouble(string left, string op, in double d)
        {
            return $"{left} {op} {d}";
        }

        /// <summary>
        /// Generates the filter condition for long.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="op">The op.</param>
        /// <param name="l">The l.</param>
        /// <returns>Expression.</returns>
        protected virtual string GenerateFilterConditionForLong(string left, string op, in long l)
        {
            return $"{left} {op} {l}";
        }

        /// <summary>
        /// Gets the getting property.
        /// </summary>
        /// <param name="expr">The expr.</param>
        /// <param name="enumType">Type of the enum.</param>
        /// <returns>Expression.</returns>
        protected virtual string GetGettingProperty(Expression expr, out Type enumType)
        {
            enumType = null;
            if (expr is ParameterExpression)
            {
                return "item";
            }

            if (expr is UnaryExpression unary && IsEnumConvert(unary))
            {
                enumType = unary.Operand.Type;
                expr = unary.Operand;
            }

            if (expr is MemberExpression member)
            {
                if (member.Expression is ParameterExpression &&
                    member.Expression.NodeType == ExpressionType.Parameter)
                {
                    return member.Member.GetName();
                }
            }

            return null;
        }

        /// <summary>
        /// Inverts the op.
        /// </summary>
        /// <param name="op">The op.</param>
        /// <returns>Expression.</returns>
        protected virtual string InvertOp(string op)
        {
            return op switch
            {
                "gt" => "lt",
                "lt" => "gt",
                "ge" => "le",
                "le" => "ge",
                _ => op
            };
        }

        /// <summary>
        /// Determines whether [is enum convert] [the specified node].
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        ///   <c>true</c> if [is enum convert] [the specified node]; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsEnumConvert(UnaryExpression node)
        {
            return node?.NodeType == ExpressionType.Convert &&
                   (node.Operand.Type.GetTypeInfo().IsEnum ||
                    Nullable.GetUnderlyingType(node.Operand.Type)?.IsEnum == true);
        }

        /// <summary>
        /// Parameter.
        /// </summary>
        protected class Parameter
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Parameter" /> class.
            /// </summary>
            /// <param name="expression">The expression.</param>
            /// <param name="value">The value.</param>
            public Parameter(ParameterExpression expression, object value)
            {
                ParameterExpression = expression;
                Value = value;
            }

            /// <summary>
            /// Gets the parameter expression.
            /// </summary>
            /// <value>
            /// The parameter expression.
            /// </value>
            public ParameterExpression ParameterExpression { get; }

            /// <summary>
            /// Gets the value.
            /// </summary>
            /// <value>
            /// The value.
            /// </value>
            public object Value { get; }
        }
    }
}