// <copyright file="TableQueryExtensions.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Azure
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using AzureStorage.Strings;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Extensions for TableQuery.
    /// </summary>
    public static class TableQueryExtensions
    {
        /// <summary>
        /// Add conditions for query. Convert properties marked as PartitionKey or RowKey as such key.
        /// </summary>
        /// <typeparam name="TEntity">Table entity.</typeparam>
        /// <param name="query">Query object.</param>
        /// <param name="expression">Condition expression.</param>
        /// <returns>Updated query.</returns>
        public static TableQuery<TEntity> Where<TEntity>(
             this TableQuery<TEntity> query,
            Expression<Func<TEntity, bool>> expression)
            where TEntity : TableEntity, new()
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var visitor = new PropertyReplacer<TEntity>();
            var newFunc = visitor.VisitAndConvert(expression);
            return query.InnerWhere(newFunc);
        }

        /// <summary>
        /// Add conditions for query. Does not convert any properties.
        /// </summary>
        /// <typeparam name="TEntity">Table entity.</typeparam>
        /// <param name="query">Query object.</param>
        /// <param name="expression">Condition expression.</param>
        /// <returns>Updated query.</returns>
        internal static TableQuery<TEntity> InnerWhere<TEntity>(
            this TableQuery<TEntity> query,
            Expression<Func<TEntity, bool>> expression)
            where TEntity : TableEntity, new()
        {
            var querystring = ConvertTop(expression.Body);
            return query.FilterString == null
                       ? query.Where(querystring)
                       : query.Where($"({query.FilterString}) and ({querystring})");
        }

        private static string CombineBinary(string op, BinaryExpression parameters, Parameter obj)
        {
            return TableQuery.CombineFilters(ConvertTop(parameters.Left, obj), op, ConvertTop(parameters.Right, obj));
        }

        private static string CombineNot(UnaryExpression parameter, Parameter obj)
        {
            return $"not ({ConvertTop(parameter.Operand, obj)})";
        }

        private static string ConvertBinary(string op, BinaryExpression expression, Parameter obj)
        {
            Type enumType;
            var left = GetGettingProperty(expression.Left, out enumType);
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

            if (expression.Left.NodeType == ExpressionType.Call)
            {
                var callExp = (MethodCallExpression)expression.Left;
                if (callExp.Method.Name == nameof(string.CompareOrdinal))
                {
                    var prop = GetGettingProperty(callExp.Arguments[0], out enumType);

                    object value;
                    if (obj == null)
                    {
                        value = Expression.Lambda(callExp.Arguments[1]).Compile().DynamicInvoke();
                    }
                    else
                    {
                        value = Expression.Lambda(callExp.Arguments[1], obj.ParameterExpression)
                                          .Compile()
                                          .DynamicInvoke(obj.Value);
                    }

                    if (enumType != null)
                    {
                        value = ConvertToEnumString(enumType, value);
                    }

                    return GenerateFilter(prop, op, value);
                }
            }

            throw new InvalidOperationException(Errors.TableEntityRequired);
        }

        private static string ConvertEnumerable(string connector, MethodCallExpression callexp)
        {
            var str = new StringBuilder();
            str.Append("(");

            var en = (IEnumerable)Expression.Lambda(callexp.Arguments[0]).Compile().DynamicInvoke();
            var subExp = (LambdaExpression)callexp.Arguments[1];

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

        private static string ConvertNext(Expression expression, Parameter obj)
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

        private static string ConvertToEnumString(Type enumType, object value)
        {
            var name = Enum.GetName(enumType, value);
            var info = enumType.GetMember(name).FirstOrDefault(m => m.DeclaringType == enumType);
            return info?.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? name;
        }

        private static string ConvertTop(Expression expression, Parameter obj = null)
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
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return ConvertNext(expression, obj);
                case ExpressionType.Call:
                    return ConvertCall((MethodCallExpression)expression);
                default:
                    throw new InvalidOperationException("Operation is not supported: " + expression.NodeType);
            }
        }

        private static string GenerateFilter(string left, string op, object value)
        {
            return value switch
            {
                null => throw new ArgumentNullException($"Null values comparison is not supported: {left}"),
                string s => TableQuery.GenerateFilterCondition(left, op, s),
                bool b => TableQuery.GenerateFilterConditionForBool(left, op, b),
                DateTime d => TableQuery.GenerateFilterConditionForDate(left, op, d),
                DateTimeOffset d => TableQuery.GenerateFilterConditionForDate(left, op, d),
                double d => TableQuery.GenerateFilterConditionForDouble(left, op, d),
                float f => TableQuery.GenerateFilterConditionForDouble(left, op, f),
                byte i => TableQuery.GenerateFilterConditionForInt(left, op, i),
                short i => TableQuery.GenerateFilterConditionForInt(left, op, i),
                int i => TableQuery.GenerateFilterConditionForInt(left, op, i),
                long l => TableQuery.GenerateFilterConditionForLong(left, op, l),
                _ => throw new InvalidOperationException($"Type is not supported: {value.GetType()}")
            };
        }

        private static string ConvertCall(MethodCallExpression exp)
        {
            if (exp.Method.DeclaringType == typeof(Enumerable))
            {
                switch (exp.Method.Name)
                {
                    case nameof(Enumerable.Any):
                        return ConvertEnumerable("or", exp);
                    case nameof(Enumerable.All):
                        return ConvertEnumerable("and", exp);
                }
            }

            throw new InvalidOperationException("Operation is not supported: " + exp.NodeType);
        }

        private static string GetGettingProperty(Expression expr, out Type enumType)
        {
            enumType = null;
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
                    return member.Member.Name;
                }
            }

            return null;
        }

        private static string InvertOp(string op)
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

        private static bool IsEnumConvert(UnaryExpression node)
        {
            return node?.NodeType == ExpressionType.Convert && node.Operand.Type.GetTypeInfo().IsEnum;
        }

        private class Parameter
        {
            public Parameter(ParameterExpression expression, object value)
            {
                ParameterExpression = expression;
                Value = value;
            }

            public ParameterExpression ParameterExpression { get; }

            public object Value { get; }
        }
    }
}