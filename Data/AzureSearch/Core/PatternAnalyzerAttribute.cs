// <copyright file="PatternAnalyzerAttribute.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    using global::Azure.Search.Documents.Indexes.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines pattern analyzer for the model.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Class)]
    public class PatternAnalyzerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PatternAnalyzerAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public PatternAnalyzerAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the lowe case terms.
        /// </summary>
        /// <value>
        /// The lowe case terms.
        /// </value>
        public bool? LoweCaseTerms { get; set; }

        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>
        /// The flags.
        /// </value>
        public List<RegexFlag> Flags { get; set; }

        /// <summary>
        /// Gets or sets the pattern.
        /// </summary>
        /// <value>
        /// The pattern.
        /// </value>
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets the stopwords.
        /// </summary>
        /// <value>
        /// The stopwords.
        /// </value>
        public IList<string> Stopwords { get; set; }
    }
}