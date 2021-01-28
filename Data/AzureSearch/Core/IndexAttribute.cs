// <copyright file="IndexAttribute.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataSearch.Core
{
    using System;

    /// <summary>
    /// Attribute for Azure search index models.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Class)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexAttribute" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public IndexAttribute(string name)
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
    }
}