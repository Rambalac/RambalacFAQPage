// <copyright file="ODataQueryableExtensions.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using ODataQueryable;
    using Pagination;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions for OData queries.
    /// </summary>
    public static class ODataQueryableExtensions
    {
        /// <summary>
        /// Gets name of property using JsonProperty and JsonPropertyName attributes.
        /// </summary>
        /// <param name="member">Member.</param>
        /// <returns>Name.</returns>
        public static string GetName(this MemberInfo member)
        {
            return member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
        }

        /// <summary>
        /// Adds property as ascending order.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="expression">Property expression.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> OrderBy<TEntity>(
            this IODataQueryable<TEntity> query,
            Expression<Func<TEntity, object>> expression)
        {
            var result = query.Clone();
            result.Order.Add(GetPropertyName(expression));
            return result;
        }

        /// <summary>
        /// Adds order columns by name.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="order">Column names.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> OrderBy<TEntity>(
            this IODataQueryable<TEntity> query,
            IEnumerable<string> order)
        {
            var result = query.Clone();
            result.Order.AddRange(order);

            return result;
        }

        /// <summary>
        /// Adds property as descending order.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="expression">Property expression.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> OrderByDescending<TEntity>(
            this IODataQueryable<TEntity> query,
            Expression<Func<TEntity, object>> expression)
        {
            var result = query.Clone();
            result.Order.Add(GetPropertyName(expression) + " desc");
            return result;
        }

        /// <summary>
        /// Set selected properties.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="select">Column names.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> Select<TEntity>(
            this IODataQueryable<TEntity> query,
            IEnumerable<string> select)
        {
            var result = query.Clone();
            result.Select.AddRange(select);

            return result;
        }

        /// <summary>
        /// Set selected properties.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="select">Column names.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> Select<TEntity>(
            this IODataQueryable<TEntity> query,
            params string[] select)
        {
            var result = query.Clone();
            result.Select.AddRange(select);

            return result;
        }

        /// <summary>
        /// Adds number items to skip.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="skip">Number of items to skip.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> Skip<TEntity>(this IODataQueryable<TEntity> query, int skip)
        {
            var result = query.Clone();
            result.Skip = skip;
            return result;
        }

        /// <summary>
        /// Adds number of items to take.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="take">Number of items to take.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> Take<TEntity>(this IODataQueryable<TEntity> query, int take)
        {
            var result = query.Clone();
            result.Take = take;
            return result;
        }

        /// <summary>
        /// Filter condition.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to add to.</param>
        /// <param name="expression">Conditional expression.</param>
        /// <returns>New query.</returns>
        public static IODataQueryable<TEntity> Where<TEntity>(
            this IODataQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> expression)
        {
            var result = query.Clone();
            result.Filter = query.Filter == null
                                ? $"({query.Context.FilterMaker.MakeFilter(expression)})"
                                : $"{query.Filter} and ({query.Context.FilterMaker.MakeFilter(expression)})";

            return result;
        }

        private static string GetPropertyName<TEntity>(Expression<Func<TEntity, object>> expression)
        {
            var body = expression.Body;
            if (body is UnaryExpression unari)
            {
                body = unari.Operand;
            }

            if (body is MemberExpression member)
            {
                return member.Member.GetName();
            }

            throw new NotImplementedException();
        }


    }

    /// <summary>
    /// Extensions to get results from OData queries.
    /// </summary>
    public static class ODataQueryableResultExtensions
    {
        /// <summary>
        /// Looks for the first item or returns null if no items.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query for items.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Item.</returns>
        public static async Task<TEntity> FirstOrDefaultAsync<TEntity>(
            this IODataQueryable<TEntity> query,
            CancellationToken cancellation = default)
        {
            var results = await query.Context.Provider.RetrieveItemsAsync(query.Take(1), cancellation);
            return results.Items.FirstOrDefault();
        }

        /// <summary>
        /// Makes AsyncEnumerable.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to convert.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Async query.</returns>
        public static IAsyncEnumerable<TEntity> ToAsyncEnumerable<TEntity>(
            this IODataQueryable<TEntity> query,
            CancellationToken cancellation = default)
        {
            return query.Context.Provider.QueryItemsAsync(query, cancellation);
        }

        /// <summary>
        /// Returns items from the query.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to query.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>A <see cref="Task{TResult}" /> representing the result of the asynchronous operation.</returns>
        public static async Task<List<TEntity>> ToListAsync<TEntity>(
            this IODataQueryable<TEntity> query,
            CancellationToken cancellation = default)
        {
            return await query.ToAsyncEnumerable(cancellation).ToListAsync(cancellation);
        }

        /// <summary>
        /// Returns page of items from the query.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query to query.</param>
        /// <param name="continuationToken">Continuation token string.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>A <see cref="Task{TResult}" /> representing the result of the asynchronous operation.</returns>
        public static async Task<PaginatedResponse<TEntity>> ToPageAsync<TEntity>(
            this IODataQueryable<TEntity> query,
            IPaginatedRequest request,
            CancellationToken cancellation = default)
        {
            var newQuery= query.Skip(request.Skip);
            newQuery.Take = request.Take;
            var items =  await query.Context.Provider.RetrieveItemsAsync(newQuery, cancellation);

            return new PaginatedResponse<TEntity>
            {
                Items = items.Items.ToList(),
                HasMore = items.HasMore,
                Take = request.Take,
                Skip = request.Skip,
                Total = items.Total,
            };
        }
    }
}