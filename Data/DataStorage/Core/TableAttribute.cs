// <copyright file="TableAttribute.cs" company="Rambalac">
// Copyright (c) Rambalac. All rights reserved.
// </copyright>

namespace DataStorage.Core
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// Defines entity Table name and optional fixed Partition and Row keys.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableAttribute" /> class.
        /// </summary>
        /// <param name="tableName">Name of Table.</param>
        public TableAttribute(string tableName)
        {
            Table = tableName;
        }

        /// <summary>
        /// Gets or sets optional Partition Key.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets optional Row Key.
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets table name for entities.
        /// </summary>
        /// <value>
        /// The table.
        /// </value>
        public string Table { get; set; }
    }
}