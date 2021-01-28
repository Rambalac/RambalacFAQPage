// <copyright file="IODataQueryable.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using System.Collections.Generic;

    /// <summary>
    /// Queryable for OData.
    /// </summary>
    /// <typeparam name="TEntity">Type of items.</typeparam>
    public interface IODataQueryable<TEntity>
    {
        /// <summary>
        /// Gets query context.
        /// </summary>
        ODataQuery Context { get; }

        /// <summary>
        /// Gets or sets filter string.
        /// </summary>
        string Filter { get; set; }

        /// <summary>
        /// Gets or sets collection of column names for order.
        /// </summary>
        List<string> Order { get; set; }

        /// <summary>
        /// Gets or sets collection of column names for select.
        /// </summary>
        List<string> Select { get; set; }

        /// <summary>
        /// Gets or sets how many items to skip.
        /// </summary>
        int Skip { get; set; }

        /// <summary>
        /// Gets or sets how many items to take.
        /// </summary>
        int? Take { get; set; }

        /// <summary>
        /// Clones current query.
        /// </summary>
        /// <returns>Clone.</returns>
        IODataQueryable<TEntity> Clone();
    }
}