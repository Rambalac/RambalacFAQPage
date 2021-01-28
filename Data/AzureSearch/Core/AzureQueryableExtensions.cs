// <copyright file="AzureQueryableExtensions.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    using System.Collections.Generic;
    using global::Azure.Search.Documents.Models;
    using ODataQueryable;

    /// <summary>
    /// Extensions for Azure search specific operations.
    /// </summary>
    public static class AzureQueryableExtensions
    {
        /// <summary>
        /// Sets type of Search query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="queryType">Type of the query.</param>
        /// <returns>Query.</returns>
        public static IODataQueryable<TEntity> QueryType<TEntity>(
            this IODataQueryable<TEntity> query,
            SearchQueryType queryType)
        {
            var result = new AzureQueryable<TEntity>(query) { QueryType = queryType };
            return result;
        }

        /// <summary>
        /// Searches the specified search.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="search">The search.</param>
        /// <param name="searchFields">The search fields.</param>
        /// <returns>Queryable.</returns>
        public static IODataQueryable<TEntity> Search<TEntity>(
            this IODataQueryable<TEntity> query,
            string search,
            IEnumerable<string> searchFields = null)
        {
            var result = new AzureQueryable<TEntity>(query);
            result.Search = search;
            if (searchFields != null)
            {
                result.SearchFields = new List<string>(searchFields);
            }

            return result;
        }

        /// <summary>
        /// Sets mode of Search query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Query.</returns>
        public static IODataQueryable<TEntity> SearchMode<TEntity>(
            this IODataQueryable<TEntity> query,
            SearchMode mode)
        {
            var result = new AzureQueryable<TEntity>(query) { SearchMode = mode };
            return result;
        }
    }
}