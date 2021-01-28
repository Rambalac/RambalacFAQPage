// <copyright file="AzureQueryable.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    using global::Azure.Search.Documents.Models;
    using ODataQueryable;
    using System.Collections.Generic;

    /// <summary>
    /// Azure search queryable.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="IODataQueryable{TEntity}" />
    public class AzureQueryable<TEntity> : IODataQueryable<TEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureQueryable{TEntity}" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AzureQueryable(ODataQuery context)
        {
            Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureQueryable{TEntity}"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        public AzureQueryable(IODataQueryable<TEntity> query)
        {
            Context = query.Context;
            Filter = query.Filter;
            Order = query.Order;
            Select = query.Select;
            Take = query.Take;
            Skip = query.Skip;

            if (query is AzureQueryable<TEntity> azq)
            {
                Search = azq.Search;
                SearchFields = azq.SearchFields;
                SearchMode = azq.SearchMode;
                QueryType = azq.QueryType;
            }
        }

        /// <inheritdoc />
        public ODataQuery Context { get; }

        /// <inheritdoc />
        public string Filter { get; set; }

        /// <inheritdoc />
        public List<string> Order { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the search.
        /// </summary>
        /// <value>
        /// The search.
        /// </value>
        public string Search { get; set; }

        /// <summary>
        /// Gets or sets the search fields.
        /// </summary>
        /// <value>
        /// The search fields.
        /// </value>
        public List<string> SearchFields { get; set; } = new List<string>();

        /// <inheritdoc />
        public List<string> Select { get; set; } = new List<string>();

        /// <inheritdoc />
        public int Skip { get; set; }

        /// <inheritdoc />
        public int? Take { get; set; }

        /// <summary>
        /// Gets or sets the search mode.
        /// </summary>
        /// <value>
        /// The search mode.
        /// </value>
        public SearchMode SearchMode { get; set; }

        /// <summary>
        /// Gets or sets the type of the query.
        /// </summary>
        /// <value>
        /// The type of the query.
        /// </value>
        public SearchQueryType QueryType { get; set; }

        /// <inheritdoc />
        public IODataQueryable<TEntity> Clone()
        {
            return new AzureQueryable<TEntity>(this);
        }
    }
}