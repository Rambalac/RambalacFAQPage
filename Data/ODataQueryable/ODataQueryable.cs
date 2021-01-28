// <copyright file="ODataQueryable.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using System.Collections.Generic;

    /// <summary>
    /// General implementation for OData query.
    /// </summary>
    /// <typeparam name="TEntity">Type of items.</typeparam>
    public class ODataQueryable<TEntity> : IODataQueryable<TEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQueryable{TEntity}" /> class.
        /// </summary>
        /// <param name="context">Execution context.</param>
        public ODataQueryable(ODataQuery context)
        {
            Context = context;
        }

        /// <inheritdoc />
        public ODataQuery Context { get; }

        /// <inheritdoc />
        public string Filter { get; set; }

        /// <inheritdoc />
        public List<string> Order { get; set; } = new List<string>();

        /// <inheritdoc />
        public List<string> Select { get; set; } = new List<string>();

        /// <inheritdoc />
        public int Skip { get; set; }

        /// <inheritdoc />
        public int? Take { get; set; }

        /// <inheritdoc />
        public IODataQueryable<TEntity> Clone()
        {
            return new ODataQueryable<TEntity>(Context)
            {
                Filter = Filter,
                Order = new List<string>(Order),
                Select = new List<string>(Select),
                Take = Take,
                Skip = Skip,
            };
        }
    }
}