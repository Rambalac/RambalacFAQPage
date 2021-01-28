// <copyright file="ITableStorage.cs" company="Rambalac">
// Copyright (c) Rambalac. All rights reserved.
// </copyright>

namespace DataStorage.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Base interface for storage mocking.
    /// </summary>
    public interface ITableStorage
    {
        /// <summary>
        /// Checks if any exists.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>True if there is any.</returns>
        Task<bool> AnyAsync<TEntity>(
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Checks if any exists.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>True if exists.</returns>
        Task<bool> AnyAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Checks if entity does exist.
        /// </summary>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="entity">Entity.</param>
        /// <returns>True if item exists.</returns>
        Task<bool> CheckExistsAsync<TEntity>(TEntity entity)
            where TEntity : TableEntity;

        /// <summary>
        /// Counts result.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity to query and count.</typeparam>
        /// <param name="query">Query to count.</param>
        /// <param name="token">Operation cancellation.</param>
        /// <returns>Number of items selected by query.</returns>
        Task<int> CountAsync<TEntity>(TableQuery<TEntity> query, CancellationToken token)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Counts the asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>Number of items.</returns>
        Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>> func, CancellationToken cancellation)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Creates new table if it does not exist.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>
        /// Task.
        /// </returns>
        Task CreateTableIfNotExistsAsync<TEntity>(CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Deletes Table entity with specific PartitionKey and RowKey.
        /// </summary>
        /// <param name="entity">Entity to delete.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Table entity.</typeparam>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Execute Table query.
        /// </summary>
        /// <typeparam name="TEntity">Table entity for query.</typeparam>
        /// <param name="query">Query.</param>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        IAsyncEnumerable<TEntity> ExecuteQueryAsync<TEntity>(
            TableQuery<TEntity> query,
            CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Query Azure Table for first or default item by expression.
        /// </summary>
        /// <param name="func">Query expression.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        Task<TEntity> FirstOrDefaultAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Firsts the or default asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<TEntity> FirstOrDefaultAsync<TEntity>(
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Returns default query for all items.
        /// </summary>
        /// <typeparam name="TEntity">Type of items.</typeparam>
        /// <returns>Query.</returns>
        string GetDefaultQuery<TEntity>();

        /// <summary>
        /// Gets Table object by type.
        /// </summary>
        /// <typeparam name="T">Type of Table.</typeparam>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>
        /// Table object.
        /// </returns>
        CloudTable GetTable<T>(string tableName = null);

        /// <summary>
        /// Get table name for the entity.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <returns>Name.</returns>
        string GetTableName<TEntity>();

        /// <summary>
        /// Inserts new entity into table. No existing entity with the same Row and Partition keys is allowed.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="entity">Entity to insert.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task<bool> InsertAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Inserts list of entities in batch.
        /// </summary>
        /// <param name="entities">Entities.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <returns>List of entities with insert flag. Flag is true if entity was inserted without conflict.</returns>
        Task<List<KeyValuePair<TEntity, bool>>> InsertAsync<TEntity>(
            IEnumerable<TEntity> entities,
            CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Inserts entity into table. If entity with the same Row and Partition keys exists it get replaced.
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task InsertOrReplaceAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Inserts or Replaces list of entities in batch.
        /// </summary>
        /// <param name="entities">Entities.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of entity.</typeparam>
        /// <returns>List of entities with insert flag. Flag is true if entity was inserted without conflict.</returns>
        Task InsertOrReplaceAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Merger values if ETag did not change.
        /// </summary>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="entity">Entity to merge. Should be previously retrieved from Storage.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>True if updated.</returns>
        Task<bool> MergeAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Merges collection of items.
        /// </summary>
        /// <typeparam name="TEntity">Time of item.</typeparam>
        /// <param name="items">Items to merge.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Task.</returns>
        Task MergeAsync<TEntity>(IEnumerable<TEntity> items, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Starts query for specific Table type.
        /// </summary>
        /// <typeparam name="TEntity">Type of Table.</typeparam>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        IAsyncEnumerable<TEntity> QueryAsync<TEntity>(CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Query Azure Table by expression.
        /// </summary>
        /// <param name="func">Query expression.</param>
        /// <param name="take">Segment size.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        IAsyncEnumerable<TEntity> QueryAsync<TEntity>(
             Expression<Func<TEntity, bool>> func,
            int take,
            CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Query Azure Table by expression.
        /// </summary>
        /// <param name="func">Query expression.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        IAsyncEnumerable<TEntity> QueryAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Starts query for specific Table type.
        /// </summary>
        /// <typeparam name="TEntity">Type of Table.</typeparam>
        /// <param name="entity">Entity with not null PartitionKey or RowKey for query.</param>
        /// <param name="token">Cancellation.</param>
        /// <returns>Asynchronous Enumerable with result.</returns>
        IAsyncEnumerable<TEntity> QueryAsync<TEntity>(
             TEntity entity,
            CancellationToken token = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Query Azure Table by expression.
        /// </summary>
        /// <param name="func">Query expression.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="segmentSize">Number of items in one segment.</param>
        /// <param name="token">Continuation token.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Segment of result items.</returns>
        Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            int segmentSize = 10,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Query Azure Table by expression.
        /// </summary>
        /// <param name="query">Query expression.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <param name="token">Continuation token.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <returns>Segment of result items.</returns>
        Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
            TableQuery<TEntity> query,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Queries the segmented asynchronous.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="segmentSize">Size of the segment.</param>
        /// <param name="token">The token.</param>
        /// <param name="cancellation">The cancellation.</param>
        /// <returns>Segment of result items.</returns>
        /// <autogeneratedoc />
        Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
            int segmentSize = 10,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Remaps the table.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="newName">The new name.</param>
        void RemapTable<T>(string newName);

        /// <summary>
        /// Replace only if entity already exists.
        /// </summary>
        /// <param name="entity">Entity to replace.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>Returns true, if replace was successful. Otherwise false if item was not found.</returns>
        Task<bool> ReplaceAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets singleton entity. Partition and row keys should be fixed in Table attribute.
        /// </summary>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>Entity, or null if not found. A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<TEntity> RetrieveAsync<TEntity>(CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets entity by row keys. Partition should be fixed in Table attribute.
        /// </summary>
        /// <param name="rowKey">Entity Row Key.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>Entity, or null if not found. A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<TEntity> RetrieveAsync<TEntity>(string rowKey, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets entity by partition and row keys.
        /// </summary>
        /// <param name="partitionKey">Entity Partition Key.</param>
        /// <param name="rowKey">Entity Row Key.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>Entity, or null if not found. A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<TEntity> RetrieveAsync<TEntity>(string partitionKey, string rowKey, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets entity by pattern entity with partition and row keys.
        /// </summary>
        /// <param name="entity">Entity with partition and row key to use as pattern.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>Entity, or null if not found. A <see cref="Task" /> representing the asynchronous operation.</returns>
        Task<TEntity> RetrieveAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets entity by pattern entity with partition and row keys.
        /// If entity does not exists it stores pattern entity and returns it as result.
        /// </summary>
        /// <param name="entity">Entity with partition and row key to use as pattern.</param>
        /// <param name="cancellation">Cancellation.</param>
        /// <typeparam name="TEntity">Type of Entity.</typeparam>
        /// <returns>
        /// Entity from Table, or patter entity if not found. A <see cref="Task" /> representing the asynchronous
        /// operation.
        /// </returns>
        Task<TEntity> RetrieveOrCreateAsync<TEntity>(TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity;
    }
}