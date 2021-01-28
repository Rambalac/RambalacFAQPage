// <copyright file="DataStorage.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AzureStorage.Strings;
    using DataStorage.Core;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// Helper class for Azure Storage Blobs and Tables.
    /// </summary>
    public class AzureTableStorage : ITableStorage
    {
        private static readonly ConcurrentDictionary<Type, TableAttribute> TypeToAttribute =
            new ConcurrentDictionary<Type, TableAttribute>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableStorage" /> class.
        /// </summary>
        /// <param name="connectionString">Azure Storage connection string.</param>
        public AzureTableStorage(string connectionString)
        {
            var tableAccount = CloudStorageAccount.Parse(connectionString);
            TableClient = tableAccount.CreateCloudTableClient();
        }

        private CloudTableClient TableClient { get; }

        /// <inheritdoc />
        public async Task<bool> AnyAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var result = await QuerySegmentedAsync(func, 1, null, cancellation).ConfigureAwait(false);
            return result.Results.Any();
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync<TEntity>(
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var result = await QuerySegmentedAsync<TEntity>(1, null, cancellation).ConfigureAwait(false);
            return result.Results.Any();
        }

        /// <inheritdoc />
        public async Task<bool> CheckExistsAsync<TEntity>(TEntity entity)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var attr = GetEntityAttribute<TEntity>();
            var partition = entity.PartitionKey ??
                            attr.PartitionKey ?? throw new ArgumentException(Errors.PartitionKeyNull);
            var key = entity.RowKey ?? attr.RowKey ?? throw new ArgumentException(Errors.RawKeyNull);

            var retrieveOperation = TableOperation.Retrieve<TEntity>(partition, key, new List<string>());

            var retrievedResult = await GetTable<TEntity>().ExecuteAsync(retrieveOperation).ConfigureAwait(false);
            return retrievedResult?.Result != null;
        }

        /// <inheritdoc />
        public async Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>> func, CancellationToken cancellation)
            where TEntity : TableEntity, new()
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var query = TableQueryExtensions.Where(Query<TEntity>(), func);
            return await CountAsync(query, cancellation);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync<TEntity>( TableQuery<TEntity> query, CancellationToken token)
            where TEntity : TableEntity, new()
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query = query.Select(new[] { "PartitionKey" });

            var table = GetTable<TEntity>();
            var requestOption = new TableRequestOptions();
            var context = new OperationContext();

            var count = 0;
            TableQuerySegment<TEntity> segment = null;
            do
            {
                segment = await table.ExecuteQuerySegmentedAsync(
                                          query,
                                          segment?.ContinuationToken,
                                          requestOption,
                                          context,
                                          token)
                                     .ConfigureAwait(false);
                if (segment == null)
                {
                    break;
                }

                token.ThrowIfCancellationRequested();
                count += segment.Results.Count;
            }
            while (segment.ContinuationToken != null);

            return count;
        }

        /// <inheritdoc />
        public async Task CreateTableIfNotExistsAsync<T>(CancellationToken cancellation)
            where T : TableEntity
        {
            var result = TableClient.GetTableReference(GetTableName<T>());
            await result.CreateIfNotExistsAsync(cancellation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        
        public async Task<bool> DeleteAsync<TEntity>( TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            PrepareEntity(entity);
            if (entity.ETag == null)
            {
                entity.ETag = "*";
            }

            var retrieveOperation = TableOperation.Delete(entity);
            try
            {
                var retrievedResult = await GetTable<TEntity>()
                                           .ExecuteAsync(
                                                retrieveOperation,
                                                new TableRequestOptions(),
                                                new OperationContext(),
                                                cancellation)
                                           .ConfigureAwait(false);
                return retrievedResult.HttpStatusCode == 200;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TEntity> ExecuteQueryAsync<TEntity>(
             TableQuery<TEntity> query,
            [EnumeratorCancellation] CancellationToken token = default)
            where TEntity : TableEntity, new()
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var table = GetTable<TEntity>();
            var requestOption = new TableRequestOptions();
            var context = new OperationContext();
            var takeCount = query.TakeCount;
            query.TakeCount = 1000;

            TableQuerySegment<TEntity> segment = null;
            do
            {
                segment = await table.ExecuteQuerySegmentedAsync(
                                          query,
                                          segment?.ContinuationToken,
                                          requestOption,
                                          context,
                                          token)
                                     .ConfigureAwait(false);

                if (segment == null)
                {
                    break;
                }

                foreach (var item in segment)
                {
                    yield return item;

                    if (takeCount != null)
                    {
                        takeCount--;
                        if (takeCount == 0)
                        {
                            break;
                        }
                    }
                }
            }
            while (segment.ContinuationToken != null);
        }

        /// <inheritdoc />
        public async Task<TEntity> FirstOrDefaultAsync<TEntity>(
            Expression<Func<TEntity, bool>> func,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var result = await QuerySegmentedAsync(func, 1, null, cancellation).ConfigureAwait(false);
            return result.Results.FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<TEntity> FirstOrDefaultAsync<TEntity>(
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var result = await QuerySegmentedAsync<TEntity>(1, null, cancellation).ConfigureAwait(false);
            return result.Results.FirstOrDefault();
        }

        /// <inheritdoc />
        public string GetDefaultQuery<TEntity>()
        {
            var attr = GetEntityAttribute<TEntity>();
            if ((attr.PartitionKey != null) && (attr.RowKey != null))
            {
                return $"PartitionKey eq '{attr.PartitionKey}' and RowKey eq '{attr.RowKey}'";
            }

            if (attr.PartitionKey != null)
            {
                return $"PartitionKey eq '{attr.PartitionKey}'";
            }

            if (attr.RowKey != null)
            {
                return $"RowKey eq '{attr.RowKey}'";
            }

            return null;
        }

        /// <param name="tableName"></param>
        /// <inheritdoc />
        
        public CloudTable GetTable<T>(string tableName = null)
        {
            var result = TableClient.GetTableReference(GetTableName<T>());
            return result;
        }

        /// <inheritdoc />
        public string GetTableName<TEntity>()
        {
            var attr = GetEntityAttribute<TEntity>();
            return attr.Table;
        }

        /// <inheritdoc />
        
        public async Task<bool> InsertAsync<TEntity>(
             TEntity entity,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            PrepareEntity(entity);
            var cloudTable = GetTable<TEntity>();
            try
            {
                var tableResult = await cloudTable.ExecuteAsync(
                                                       TableOperation.Insert(entity),
                                                       new TableRequestOptions(),
                                                       new OperationContext(),
                                                       cancellation)
                                                  .ConfigureAwait(false);
                if (tableResult.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    return false;
                }

                ProcessResult(tableResult);
                return true;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                return false;
            }
        }

        /// <inheritdoc />
        
        public async Task<List<KeyValuePair<TEntity, bool>>> InsertAsync<TEntity>(
             IEnumerable<TEntity> entity,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var attr = GetEntityAttribute<TEntity>();
            var cloudTable = GetTable<TEntity>();
            var result = new List<KeyValuePair<TEntity, bool>>();
            var batch = new TableBatchOperation();

            async Task ExecuteAsync(TableBatchOperation batchOperations)
            {
                var tableResult = await cloudTable.ExecuteBatchAsync(
                                                       batchOperations,
                                                       new TableRequestOptions(),
                                                       new OperationContext(),
                                                       cancellation)
                                                  .ConfigureAwait(false);
                result.AddRange(
                    tableResult.Select(
                        (rowResult, index) => new KeyValuePair<TEntity, bool>(
                            (TEntity)batchOperations[index].Entity,
                            rowResult.HttpStatusCode != (int)HttpStatusCode.Conflict)));
            }

            try
            {
                foreach (var en in entity)
                {
                    PrepareEntity(en, attr);

                    batch.Add(TableOperation.Insert(en));
                    if (batch.Count == 100)
                    {
                        await ExecuteAsync(batch).ConfigureAwait(false);
                        batch = new TableBatchOperation();
                    }
                }

                if (batch.Any())
                {
                    await ExecuteAsync(batch).ConfigureAwait(false);
                }

                return result;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                return result;
            }
        }

        /// <inheritdoc />
        
        public async Task InsertOrReplaceAsync<TEntity>(
             IEnumerable<TEntity> entity,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            await BatchAsync(entity, TableOperation.InsertOrReplace, cancellation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        
        public async Task InsertOrReplaceAsync<TEntity>( TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            PrepareEntity(entity);
            var cloudTable = GetTable<TEntity>();
            var insertOrReplace = TableOperation.InsertOrReplace(entity);
            var tableResult = await cloudTable.ExecuteAsync(
                                                   insertOrReplace,
                                                   new TableRequestOptions(),
                                                   new OperationContext(),
                                                   cancellation)
                                              .ConfigureAwait(false);
            ProcessResult(tableResult);
        }

        /// <inheritdoc />
        
        public async Task MergeAsync<TEntity>(IEnumerable<TEntity> entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            await BatchAsync(entity, TableOperation.Merge, cancellation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        
        public async Task<bool> MergeAsync<TEntity>( TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            PrepareEntity(entity);
            var cloudTable = GetTable<TEntity>();
            var merge = TableOperation.Merge(entity);
            var tableResult = await cloudTable.ExecuteAsync(
                                                   merge,
                                                   new TableRequestOptions(),
                                                   new OperationContext(),
                                                   cancellation)
                                              .ConfigureAwait(false);
            if (tableResult.HttpStatusCode == 412)
            {
                return false;
            }

            ProcessResult(tableResult);
            return true;
        }

        /// <inheritdoc />
        
        public IAsyncEnumerable<TEntity> QueryAsync<TEntity>(TEntity entity, CancellationToken token = default)
            where TEntity : TableEntity, new()
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return ExecuteQueryAsync(Query(entity), token);
        }

        /// <inheritdoc />
        
        public IAsyncEnumerable<TEntity> QueryAsync<TEntity>(
             Expression<Func<TEntity, bool>> func,
            CancellationToken token = default)
            where TEntity : TableEntity, new()
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var query = TableQueryExtensions.Where(Query<TEntity>(), func);
            return ExecuteQueryAsync(query, token);
        }

        /// <inheritdoc />
        
        public IAsyncEnumerable<TEntity> QueryAsync<TEntity>(
             Expression<Func<TEntity, bool>> func,
            int take,
            CancellationToken token = default)
            where TEntity : TableEntity, new()
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var query = TableQueryExtensions.Where(Query<TEntity>().Take(take), func);
            return ExecuteQueryAsync(query, token);
        }

        /// <inheritdoc />
        
        public IAsyncEnumerable<TEntity> QueryAsync<TEntity>(CancellationToken token = default)
            where TEntity : TableEntity, new()
        {
            return ExecuteQueryAsync(Query<TEntity>(), token);
        }

        /// <inheritdoc />
        
        public async Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
             Expression<Func<TEntity, bool>> func,
            int segmentSize = 10,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var query = TableQueryExtensions.Where(Query<TEntity>(), func);
            query.TakeCount = segmentSize;

            var table = GetTable<TEntity>();
            var requestOption = new TableRequestOptions();
            var context = new OperationContext();

            return await table.ExecuteQuerySegmentedAsync(query, token, requestOption, context, cancellation)
                              .ConfigureAwait(false);
        }

        /// <inheritdoc />
        
        public async Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
            int segmentSize = 10,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var query = Query<TEntity>();
            query.TakeCount = segmentSize;

            var table = GetTable<TEntity>();
            var requestOption = new TableRequestOptions();
            var context = new OperationContext();

            return await table.ExecuteQuerySegmentedAsync(query, token, requestOption, context, cancellation)
                              .ConfigureAwait(false);
        }

        /// <inheritdoc />
        
        public async Task<TableQuerySegment<TEntity>> QuerySegmentedAsync<TEntity>(
            TableQuery<TEntity> query,
            TableContinuationToken token = null,
            CancellationToken cancellation = default)
            where TEntity : TableEntity, new()
        {
            var table = GetTable<TEntity>();
            var requestOption = new TableRequestOptions();
            var context = new OperationContext();

            return await table.ExecuteQuerySegmentedAsync(query, token, requestOption, context, cancellation)
                              .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void RemapTable<T>(string newName)
        {
            var attr = GetEntityAttribute<T>();
            attr.Table = newName;
        }

        /// <inheritdoc />
        
        public async Task<bool> ReplaceAsync<TEntity>( TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            PrepareEntity(entity);
            entity.ETag = "*";
            var tableResult = await GetTable<TEntity>()
                                   .ExecuteAsync(
                                        TableOperation.Replace(entity),
                                        new TableRequestOptions(),
                                        new OperationContext(),
                                        cancellation)
                                   .ConfigureAwait(false);

            if (tableResult.HttpStatusCode == 404)
            {
                return false;
            }

            ProcessResult(tableResult);
            return true;
        }

        /// <inheritdoc />
        
        public async Task<TEntity> RetrieveAsync<TEntity>(
            string partitionKey,
            string rowKey,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            var retrievedResult = await GetTable<TEntity>()
                                       .ExecuteAsync(
                                            retrieveOperation,
                                            new TableRequestOptions(),
                                            new OperationContext(),
                                            cancellation)
                                       .ConfigureAwait(false);
            return retrievedResult?.Result as TEntity;
        }

        /// <inheritdoc />
        
        public Task<TEntity> RetrieveAsync<TEntity>( TEntity entity, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var attr = GetEntityAttribute<TEntity>();
            var partition = entity.PartitionKey ??
                            attr.PartitionKey ?? throw new ArgumentException(Errors.PartitionKeyNull);
            var key = entity.RowKey ?? attr.RowKey ?? throw new ArgumentException(Errors.RawKeyNull);
            return RetrieveAsync<TEntity>(partition, key, cancellation);
        }

        /// <inheritdoc />
        
        public Task<TEntity> RetrieveAsync<TEntity>(string rowKey, CancellationToken cancellation)
            where TEntity : TableEntity
        {
            return RetrieveAsync<TEntity>(GetEntityPartition<TEntity>(), rowKey, cancellation);
        }

        /// <inheritdoc />
        
        public Task<TEntity> RetrieveAsync<TEntity>(CancellationToken cancellation)
            where TEntity : TableEntity
        {
            var attr = GetEntityAttribute<TEntity>();
            return RetrieveAsync<TEntity>(
                attr.PartitionKey ?? throw new ArgumentException(Errors.PartitionKeyNull),
                attr.RowKey ?? throw new ArgumentException(Errors.RawKeyNull),
                cancellation);
        }

        /// <inheritdoc />
        
        public async Task<TEntity> RetrieveOrCreateAsync<TEntity>(
             TEntity entity,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var attr = GetEntityAttribute<TEntity>();
            var partition = entity.PartitionKey ??
                            attr.PartitionKey ?? throw new ArgumentException(Errors.PartitionKeyNull);
            var key = entity.RowKey ?? attr.RowKey ?? throw new ArgumentException(Errors.RawKeyNull);
            return await RetrieveAsync<TEntity>(partition, key, cancellation).ConfigureAwait(false) ?? entity;
        }

        private static TableAttribute GetEntityAttribute<T>()
        {
            return TypeToAttribute.GetOrAdd(
                typeof(T),
                t => t.GetTypeInfo().GetCustomAttribute<TableAttribute>() ??
                     throw new ArgumentException($"Type {typeof(T)} does not have Table attribute"));
        }

        private static string GetEntityPartition<T>()
        {
            return GetEntityAttribute<T>().PartitionKey ??
                   throw new ArgumentException($"Type {typeof(T)} does not have default Partition");
        }

        private static void PrepareEntity<TEntity>(TEntity entity)
            where TEntity : TableEntity
        {
            var attr = GetEntityAttribute<TEntity>();
            if (entity.PartitionKey == null)
            {
                entity.PartitionKey = attr.PartitionKey;
            }

            if (entity.RowKey == null)
            {
                entity.RowKey = attr.RowKey;
            }
        }

        private static void PrepareEntity<TEntity>(TEntity entity, TableAttribute attr)
            where TEntity : TableEntity
        {
            if (entity.PartitionKey == null)
            {
                entity.PartitionKey = attr.PartitionKey;
            }

            if (entity.RowKey == null)
            {
                entity.RowKey = attr.RowKey;
            }
        }

        private static void ProcessResult(TableResult result)
        {
            switch (result.HttpStatusCode)
            {
                case (int)HttpStatusCode.OK:
                case (int)HttpStatusCode.Accepted:
                case (int)HttpStatusCode.Created:
                case (int)HttpStatusCode.NoContent:
                    return;

                case (int)HttpStatusCode.Conflict: throw new ArgumentException(Errors.Conflict);

                default: throw new Exception($"Request failed: {result.HttpStatusCode} - {result.Result}");
            }
        }

        private static TableQuery<TEntity> Query<TEntity>(TEntity entity)
            where TEntity : TableEntity, new()
        {
            var attr = GetEntityAttribute<TEntity>();
            var query = new TableQuery<TEntity>();
            if ((attr.PartitionKey != null) && (attr.RowKey == null) && (entity.RowKey != null))
            {
                return query.InnerWhere(i => (i.PartitionKey == attr.PartitionKey) && (i.RowKey == entity.RowKey));
            }

            if ((attr.PartitionKey == null) && (attr.RowKey != null) && (entity.PartitionKey != null))
            {
                return query.InnerWhere(i => (i.RowKey == attr.RowKey) && (i.PartitionKey == entity.PartitionKey));
            }

            return Query<TEntity>();
        }

        private static TableQuery<TEntity> Query<TEntity>()
            where TEntity : TableEntity, new()
        {
            var attr = GetEntityAttribute<TEntity>();

            var query = new TableQuery<TEntity>();
            if ((attr.PartitionKey != null) && (attr.RowKey != null))
            {
                query = query.InnerWhere(i => (i.PartitionKey == attr.PartitionKey) && (i.RowKey == attr.RowKey));
            }
            else if ((attr.PartitionKey != null) && (attr.RowKey == null))
            {
                query = query.InnerWhere(i => i.PartitionKey == attr.PartitionKey);
            }
            else if ((attr.PartitionKey == null) && (attr.RowKey != null))
            {
                query = query.InnerWhere(i => i.RowKey == attr.RowKey);
            }

            return query;
        }

        private async Task BatchAsync<TEntity>(
             IEnumerable<TEntity> entity,
            Func<TEntity, TableOperation> operation,
            CancellationToken cancellation)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var attr = GetEntityAttribute<TEntity>();
            var cloudTable = GetTable<TEntity>();
            var batch = new TableBatchOperation();
            foreach (var en in entity)
            {
                PrepareEntity(en, attr);
                batch.Add(operation(en));
                if (batch.Count == 100)
                {
                    await cloudTable.ExecuteBatchAsync(
                                         batch,
                                         new TableRequestOptions(),
                                         new OperationContext(),
                                         cancellation)
                                    .ConfigureAwait(false);
                    batch = new TableBatchOperation();
                }
            }

            if (batch.Any())
            {
                await cloudTable.ExecuteBatchAsync(
                                     batch,
                                     new TableRequestOptions(),
                                     new OperationContext(),
                                     cancellation)
                                .ConfigureAwait(false);
            }
        }
    }
}