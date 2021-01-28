// <copyright file="ODataQuery.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    /// <summary>
    /// Content for query.
    /// </summary>
    public class ODataQuery
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQuery" /> class.
        /// </summary>
        /// <param name="provider">Provider implementation for data.</param>
        public ODataQuery(IODataQueryProvider provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// Gets or sets the filter maker.
        /// </summary>
        /// <value>
        /// The filter maker.
        /// </value>
        public BooleanExpressionExtensions FilterMaker { get; set; } = new BooleanExpressionExtensions();

        /// <summary>
        /// Gets data provider.
        /// </summary>
        public IODataQueryProvider Provider { get; }

        /// <summary>
        /// Query root.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <returns>Queryable.</returns>
        public IODataQueryable<TEntity> For<TEntity>()
        {
            return new ODataQueryable<TEntity>(this);
        }
    }
}