// <copyright file="ODataResult.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using System.Collections.Generic;

    /// <summary>
    /// General query result.
    /// </summary>
    /// <typeparam name="TEntity">Type of item.</typeparam>
    public class ODataResult<TEntity>
    {      
        /// <summary>
        /// Gets a value indicating whether has more items.
        /// </summary>
        public virtual bool HasMore => false;

        /// <summary>
        /// Gets or sets items.
        /// </summary>
        public IEnumerable<TEntity> Items { get; set; }

        /// <summary>
        /// Gets or sets total number of results or null if not known.
        /// </summary>
        public long? Total { get; set; }
    }
}