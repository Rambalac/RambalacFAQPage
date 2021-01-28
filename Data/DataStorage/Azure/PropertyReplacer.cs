// <copyright file="PropertyReplacer.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Azure
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization;
    using AzureStorage.Strings;
    using DataStorage.Core;
    using Microsoft.Azure.Cosmos.Table;

    /// <inheritdoc />
    /// <summary>
    /// Rewrites expression to use RowKey and PartitionKey instead of mapping get-properties.
    /// </summary>
    /// <typeparam name="TEntity">Table entity.</typeparam>
    internal class PropertyReplacer<TEntity> : ExpressionVisitor
    {
        /// <summary>
        /// Start conversion.
        /// </summary>
        /// <param name="root">Expression to convert.</param>
        /// <returns>Converted expression.</returns>
        public Expression<Func<TEntity, bool>> VisitAndConvert(Expression<Func<TEntity, bool>> root)
        {
            return (Expression<Func<TEntity, bool>>)VisitLambda(root);
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left is UnaryExpression left && IsEnumConvert(left) && node.Right.Type == typeof(int))
            {
                var leftMember = ReplaceMember(left.Operand as MemberExpression);
                if (leftMember != null)
                {
                    return Expression.MakeBinary(
                        node.NodeType,
                        leftMember,
                        GetEnumString(node.Right, left.Operand.Type));
                }
            }

            if (node.Right is UnaryExpression right && IsEnumConvert(right) && node.Left.Type == typeof(int))
            {
                var rightMember = ReplaceMember(node.Right as MemberExpression);
                if (rightMember != null)
                {
                    return Expression.MakeBinary(
                        node.NodeType,
                        GetEnumString(node.Left, right.Operand.Type),
                        rightMember);
                }
            }

            var leftKey = ReplaceMember(node.Left as MemberExpression);
            if (leftKey != null && node.Right.Type != typeof(string))
            {
                if (node.NodeType != ExpressionType.Equal && node.NodeType != ExpressionType.NotEqual)
                {
                    throw new InvalidOperationException(Errors.KeyOperationsSupported);
                }

                return Expression.MakeBinary(node.NodeType, leftKey, GetString(node.Right));
            }

            var rightKey = ReplaceMember(node.Right as MemberExpression);
            if (rightKey != null && node.Left.Type != typeof(string))
            {
                if (node.NodeType != ExpressionType.Equal && node.NodeType != ExpressionType.NotEqual)
                {
                    throw new InvalidOperationException(Errors.KeyOperationsSupported);
                }

                return Expression.MakeBinary(node.NodeType, GetString(node.Left), rightKey);
            }

            return base.VisitBinary(node);
        }

        /// <inheritdoc />
        protected override Expression VisitMember(MemberExpression node)
        {
            return ReplaceMember(node) ?? base.VisitMember(node);
        }

        private static Expression GetEnumString(Expression node, Type type)
        {
            var nodeValue = Expression.Lambda(node).Compile().DynamicInvoke();
            var enumVal = Enum.ToObject(type, nodeValue);
            var name = enumVal.ToString();
            var memInfo = type.GetRuntimeField(name);
            var attribute = memInfo.GetCustomAttribute<EnumMemberAttribute>();
            var value = attribute?.Value ?? name;
            return Expression.Constant(value);
        }

        private static bool IsEnumConvert(UnaryExpression node)
        {
            return node?.NodeType == ExpressionType.Convert && node.Operand.Type.GetTypeInfo().IsEnum;
        }

        private static MemberExpression ReplaceMember(MemberExpression node)
        {
            if (node?.Member.GetCustomAttribute<RowKeyAttribute>() != null)
            {
                return Expression.MakeMemberAccess(node.Expression, typeof(TableEntity).GetRuntimeProperty("RowKey"));
            }

            if (node?.Member.GetCustomAttribute<PartitionKeyAttribute>() != null)
            {
                return Expression.MakeMemberAccess(
                    node.Expression,
                    typeof(TableEntity).GetRuntimeProperty("PartitionKey"));
            }

            return null;
        }

        private Expression GetString(Expression node)
        {
            var str = Expression.Lambda(node).Compile().DynamicInvoke().ToString();
            return Expression.Constant(str);
        }
    }
}