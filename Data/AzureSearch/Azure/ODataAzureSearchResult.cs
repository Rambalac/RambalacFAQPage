// <copyright file="AzureSearch.cs" company="Rambalac">
// Copyright (c) Rambalac. All rights reserved.
// </copyright>

namespace DataSearch.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using DataSearch.Core;
    using global::Azure;
    using global::Azure.Search.Documents.Models;
    using ODataQueryable;

    /// <summary>
    /// Model for search result.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="ODataResult{TEntity}" />
    public class ODataAzureSearchResult<TEntity> : ODataResult<TEntity>
    {
        public static async Task<ODataAzureSearchResult<TEntity>> MakeResultAsync(Response<SearchResults<TEntity>> response, CancellationToken cancellation)
        {
            var result =  new ODataAzureSearchResult<TEntity>();
            var results = response.Value.GetResultsAsync();
            if (typeof(IResourceWithScore).IsAssignableFrom(typeof(TEntity)))
            {
                result.Items = await results.Select(a =>
                {
                    var res = a.Document;
                    ((IResourceWithScore)res).Score = a.Score;
                    return res;
                }).ToListAsync(cancellation);
            }
            else
            {
                result.Items = await results.Select(a => a.Document).ToListAsync(cancellation);
            }

            result.Total = response.Value.TotalCount;
            return result;
        }
    }
}