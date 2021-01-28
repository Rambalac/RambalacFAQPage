// <copyright file="IResourceWithScore.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    /// <summary>
    /// Abstraction for items with score property.
    /// </summary>
    public interface IResourceWithScore
    {
        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        public double? Score { get; set; }
    }
}