// <copyright file="PartitionKeyAttribute.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Core
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// Marks property used for PartitionKey.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PartitionKeyAttribute : Attribute
    {
    }
}