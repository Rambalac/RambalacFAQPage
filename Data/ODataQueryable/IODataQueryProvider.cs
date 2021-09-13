// <copyright file="IODataQueryProvider.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace ODataQueryable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction for data provider.
    /// </summary>
    public interface IODataQueryProvider
    {
        /// <summary>
        /// Retrieves items.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="query">Query.</param>
        /// <param name="prevResult">Previous result.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>A <see cref="Task{TResult}" /> representing the result of the asynchronous operation.</returns>
        Task<ODataResult<TEntity>> RetrieveItemsAsync<TEntity>(
            IODataQueryable<TEntity> query,
            CancellationToken cancellation);

        IAsyncEnumerable<TEntity> QueryItemsAsync<TEntity>(
            IODataQueryable<TEntity> query,
            CancellationToken cancellation);
    }
}