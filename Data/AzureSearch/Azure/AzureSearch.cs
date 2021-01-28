// <copyright file="AzureSearch.cs" company="Rambalac">
// Copyright (c) Rambalac. All rights reserved.
// </copyright>

namespace DataSearch.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using DataSearch.Core;
    using global::Azure;
    using global::Azure.Search.Documents;
    using global::Azure.Search.Documents.Indexes;
    using global::Azure.Search.Documents.Indexes.Models;
    using Microsoft.Extensions.Configuration;
    using ODataQueryable;

    /// <summary>
    /// Implementation for Data search.
    /// </summary>
    public class AzureSearch : IDataSearch
    {
        private static readonly ConcurrentDictionary<Type, EntityInfo> TypeToAttribute =
            new ConcurrentDictionary<Type, EntityInfo>();

        private readonly SearchIndexClient client;

        private readonly IConfiguration config;
        private readonly SearchIndexerClient indexer;

        private readonly ODataQuery queryBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSearch" /> class.
        /// </summary>
        /// <param name="endpoint">Azure search Endpoint.</param>
        /// <param name="key">Service Key.</param>
        /// <param name="config">Configuration.</param>
        public AzureSearch(Uri endpoint, string key, IConfiguration config = null)
        {
            this.config = config;
            var credential = new AzureKeyCredential(key);
            client = new SearchIndexClient(endpoint, credential);
            indexer = new SearchIndexerClient(endpoint, credential);
            queryBuilder = new ODataQuery(this) { FilterMaker = new AzureSearchFilterMaker(), };
        }

        /// <inheritdoc />
        public async Task AddOrUpdateItemAsync<TEntity>(TEntity item, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            await info.Index.MergeOrUploadDocumentsAsync(new[] { item }, cancellationToken: cancellation);
        }

        /// <inheritdoc />
        public async Task AddOrUpdateItemsAsync<TEntity>(IEnumerable<TEntity> items, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            await info.Index.MergeOrUploadDocumentsAsync(items, cancellationToken: cancellation);
        }

        /// <inheritdoc />
        public async Task CreateIndexIfMissingAsync<TEntity>(CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();
            var index = await GetIndexAsync(info, cancellation);
            if (index == null)
            {
                var definition = new SearchIndex(info.IndexName)
                {
                    Fields = new FieldBuilder().Build(typeof(TEntity)),
                };

                var analyzers = new List<LexicalAnalyzer>();
                analyzers.AddRange(
                    typeof(TEntity)
                       .GetCustomAttributes<PatternAnalyzerAttribute>()
                       .Select(
                            a => new PatternAnalyzer(a.Name)
                            {
                                LowerCaseTerms = a.LoweCaseTerms,
                                Pattern = a.Pattern,
                            }));

                //TODO find the way to add analizers.
                await client.CreateIndexAsync(definition, cancellation);
            }
        }

        /// <inheritdoc />
        public async Task DeleteIndexIfExistsAsync<TEntity>(CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();
            if (await GetIndexAsync(info, cancellation) != null)
            {
                await client.DeleteIndexAsync(info.IndexName, cancellation);
            }
        }

        /// <inheritdoc />
        public async Task DeleteItemAsync<TEntity>(TEntity item, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            await info.Index.DeleteDocumentsAsync(new[] { item }, cancellationToken: cancellation);
        }

        /// <inheritdoc />
        public IODataQueryable<TEntity> For<TEntity>()
        {
            return queryBuilder.For<TEntity>();
        }

        /// <inheritdoc />
        public async Task<TEntity> GetDocumentAsync<TEntity>(string key, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();
            return await info.Index
                             .GetDocumentAsync<TEntity>(key, cancellationToken: cancellation);
        }

        /// <inheritdoc />
        public async Task<bool> IsValidIndexAsync<TEntity>(CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();
            var index = await GetIndexAsync(info, cancellation);
            if (index == null)
            {
                return false;
            }

            var fields = new FieldBuilder().Build(typeof(TEntity));
            if (fields.Count != index.Fields.Count)
            {
                return false;
            }

            return fields.All(f => Compare(f, index.Fields.SingleOrDefault(i => i.Name == f.Name)));
        }

        public async Task<ODataResult<TEntity>> RetrieveItemsAsync<TEntity>(IODataQueryable<TEntity> query, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            var parameters = new SearchOptions
            {
                Filter = query.Filter,
                Size = query.Take,
                Skip = query.Skip,
                IncludeTotalCount = true,
            };
            AddRange(parameters.OrderBy, query.Order);
            AddRange(parameters.Select, query.Select);

            string search = null;

            if (query is AzureQueryable<TEntity> azq)
            {
                AddRange(parameters.SearchFields, azq.SearchFields);
                parameters.SearchMode = azq.SearchMode;
                parameters.QueryType = azq.QueryType;
                search = azq.Search;
            }

            var response = await info.Index.SearchAsync<TEntity>(search, parameters, cancellation);

            return await ODataAzureSearchResult<TEntity>.MakeResultAsync(response, cancellation);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TEntity> QueryItemsAsync<TEntity>(
            IODataQueryable<TEntity> query,
            [EnumeratorCancellation] CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            var parameters = new SearchOptions
            {
                Filter = query.Filter,
                Size = query.Take,
                Skip = query.Skip,
                IncludeTotalCount = true,
            };
            AddRange(parameters.OrderBy, query.Order);
            AddRange(parameters.Select, query.Select);

            string search = null;

            if (query is AzureQueryable<TEntity> azq)
            {
                AddRange(parameters.SearchFields, azq.SearchFields);
                parameters.SearchMode = azq.SearchMode;
                parameters.QueryType = azq.QueryType;
                search = azq.Search;
            }

            var response = await info.Index.SearchAsync<TEntity>(search, parameters, cancellation);
            var results = response.Value.GetResultsAsync();

            if (typeof(IResourceWithScore).IsAssignableFrom(typeof(TEntity)))
            {
                await foreach (var res in results)
                {
                    var result = res.Document;
                    ((IResourceWithScore)result).Score = res.Score;
                    yield return result;
                }
            }
            else
            {
                await foreach (var res in results)
                {
                    yield return res.Document;
                }
            }
        }

        /// <inheritdoc />
        public async Task UpdateItemsAsync<TEntity>(IEnumerable<TEntity> items, CancellationToken cancellation)
        {
            var info = GetEntityInfo<TEntity>();

            await info.Index.MergeDocumentsAsync(items, cancellationToken: cancellation);
        }

        /// <inheritdoc />
        public async Task ValidateOrRebuildAsync<TEntity>(
            Func<CancellationToken, Task> action,
            CancellationToken cancellation)
        {
            if (!await IsValidIndexAsync<TEntity>(cancellation))
            {
                await DeleteIndexIfExistsAsync<TEntity>(cancellation).ConfigureAwait(false);
                await CreateIndexIfMissingAsync<TEntity>(cancellation).ConfigureAwait(false);

                await action(cancellation);
            }
        }

        /// <inheritdoc />
        public async Task ValidateOrRebuildAsync<TEntity>(
            CancellationToken cancellation)
        {
            if (!await IsValidIndexAsync<TEntity>(cancellation))
            {
                await DeleteIndexIfExistsAsync<TEntity>(cancellation).ConfigureAwait(false);
                await CreateIndexIfMissingAsync<TEntity>(cancellation).ConfigureAwait(false);
            }
        }

        private void AddRange(IList<string> to, List<string> from)
        {
            foreach (var s in from)
            {
                to.Add(s);
            }
        }

        private bool Compare(SearchField a, SearchField b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            return a.IsKey == b.IsKey &&
                   a.Type == b.Type &&
                   a.IsFacetable == b.IsFacetable &&
                   a.IsFilterable == b.IsFilterable &&
                   a.IsSearchable == b.IsSearchable &&
                   a.IsSortable == b.IsSortable;
        }

        private EntityInfo GetEntityInfo<T>()
        {
            string GetIndexName(string name)
            {
                var result = name;
                if (config != null && result.StartsWith('%'))
                {
                    result = config[result.Trim('%')];
                }

                return result.ToLower();
            }

            return TypeToAttribute.GetOrAdd(
                typeof(T),
                t =>
                {
                    var attr = t.GetTypeInfo().GetCustomAttribute<IndexAttribute>() ??
                               throw new ArgumentException($"Type {typeof(T)} does not have IndexAttribute");
                    var indexName = GetIndexName(attr.Name);
                    return new EntityInfo
                    {
                        Attribute = attr,
                        Index = client.GetSearchClient(indexName),
                        IndexName = indexName,
                    };
                });
        }

        private async Task<SearchIndex> GetIndexAsync(EntityInfo info, CancellationToken cancellationToken)
        {
            try
            {
                var response = await client.GetIndexAsync(info.IndexName, cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        private class EntityInfo
        {
            public IndexAttribute Attribute { get; set; }

            public SearchClient Index { get; set; }

            public string IndexName { get; set; }
        }
    }
}