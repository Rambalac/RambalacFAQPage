// <copyright file="AzureSearchFilterMaker.cs" company="Rambalac">
// Copyright (c) Rambalac. All rights reserved.
// </copyright>

namespace DataSearch.Azure
{
    using ODataQueryable;
    using System;

    /// <summary>
    /// Extension for azure search linq converter.
    /// </summary>
    /// <seealso cref="BooleanExpressionExtensions" />
    public class AzureSearchFilterMaker : BooleanExpressionExtensions
    {
        /// <inheritdoc />
        protected override string GenerateFilterConditionForDate(string left, string op, in DateTimeOffset d)
        {
            return $"{left} {op} {d:O}";
        }
    }
}