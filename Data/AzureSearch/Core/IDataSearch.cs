// <copyright file="IDataSearch.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    using ODataQueryable;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction for data searching.
    /// </summary>
    /// <seealso cref="IODataQueryProvider" />
    public interface IDataSearch : IODataQueryProvider
    {
        /// <summary>
        /// Adds or updates item by key.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="item">Item.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task AddOrUpdateItemAsync<TEntity>(TEntity item, CancellationToken cancellation);

        /// <summary>
        /// Adds or updates item by key.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="item">Item.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task AddOrUpdateItemsAsync<TEntity>(IEnumerable<TEntity> item, CancellationToken cancellation);

        /// <summary>
        /// Creates the index if missing asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task CreateIndexIfMissingAsync<TEntity>(CancellationToken cancellation);

        /// <summary>
        /// Deletes the index if exists asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task DeleteIndexIfExistsAsync<TEntity>(CancellationToken cancellation);

        /// <summary>
        /// Starts query for items.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <returns>Query.</returns>
        IODataQueryable<TEntity> For<TEntity>();

        /// <summary>
        /// Validates if index matches entity.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>True if index matches entity.</returns>
        Task<bool> IsValidIndexAsync<TEntity>(CancellationToken cancellation);

        /// <summary>
        /// Adds or updates items by key.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="items">Items.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task UpdateItemsAsync<TEntity>(IEnumerable<TEntity> items, CancellationToken cancellation);

        /// <summary>
        /// Validates index and rebuilds if does not match.
        /// </summary>
        /// <typeparam name="TEntity">Type of the item.</typeparam>
        /// <param name="action">Async func to rebuild index content.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task ValidateOrRebuildAsync<TEntity>(Func<CancellationToken, Task> action, CancellationToken cancellation);

        /// <summary>
        /// Validates index and rebuilds if does not match.
        /// </summary>
        /// <typeparam name="TEntity">Type of item.</typeparam>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task ValidateOrRebuildAsync<TEntity>(
            CancellationToken cancellation);

        /// <summary>
        /// Deletes item from index.
        /// </summary>
        /// <typeparam name="TEntity">Type of item.</typeparam>
        /// <param name="item">Item to delete. Only key is needed.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task DeleteItemAsync<TEntity>(TEntity item, CancellationToken cancellation);

        /// <summary>
        /// Gets the document asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>Document.</returns>
        Task<TEntity> GetDocumentAsync<TEntity>(string key, CancellationToken cancellation);
    }
}