// <copyright file="BooleanColumns.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Core
{
    using System;

    /// <summary>
    /// Marks property as set of boolean columns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BooleanColumns : Attribute
    {
    }
}