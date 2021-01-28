// <copyright file="ComplexTableEntity.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using Microsoft.Azure.Cosmos.Table;

    /// <inheritdoc />
    /// <summary>
    /// Extension of <see cref="TableEntity" /> with fields JSON serialization.
    /// </summary>
    public class ComplexTableEntity : TableEntity
    {
        private static readonly HashSet<string> Excluded = new HashSet<string>
                                                           {
                                                               nameof(PartitionKey),
                                                               nameof(RowKey),
                                                               nameof(ETag),
                                                               nameof(Timestamp),
                                                           };

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexTableEntity" /> class.
        /// </summary>

        protected ComplexTableEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexTableEntity" /> class.
        /// </summary>
        /// <param name="partitionKey">Partition Key.</param>
        /// <param name="rowKey">Row Key.</param>
        protected ComplexTableEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        /// <inheritdoc />
        public override void ReadEntity(
             IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            foreach (var prop in GetType()
                                .GetRuntimeProperties()
                                .Where(p => p.CanWrite && p.GetCustomAttribute<IgnorePropertyAttribute>() == null))
            {
                if (prop.GetCustomAttribute<BooleanColumns>() != null)
                {
                    var constructor = prop.PropertyType.GetConstructor(new[] { typeof(IEnumerable<string>) });
                    if (constructor == null)
                    {
                        continue;
                    }

                    var prefix = prop.Name + "_";
                    var value = constructor.Invoke(
                        new object[]
                        {
                            properties.Keys.Where(k => k.StartsWith(prefix))
                                      .Select(k => k.Substring(prefix.Length)),
                        });
                    prop.SetValue(this, value);
                }

                if (properties.TryGetValue(prop.Name, out var entity))
                {
                    switch (entity.PropertyType)
                    {
                        case EdmType.Binary:
                            prop.SetValue(this, entity.BinaryValue);
                            break;
                        case EdmType.String:
                            if (typeof(string).GetTypeInfo().IsAssignableFrom(prop.PropertyType.GetTypeInfo()))
                            {
                                prop.SetValue(this, entity.StringValue);
                            }
                            else if (prop.PropertyType.IsEnum)
                            {
                                prop.SetValue(this, Enum.Parse(prop.PropertyType, entity.StringValue, true));
                            }
                            else
                            {
                                var underType = Nullable.GetUnderlyingType(prop.PropertyType);
                                if (underType?.IsEnum == true)
                                {
                                    prop.SetValue(this, Enum.Parse(underType, entity.StringValue, true));
                                }
                                else
                                {
                                    prop.SetValue(
                                        this,
                                        JsonSerializer.Deserialize(entity.StringValue, prop.PropertyType));
                                }
                            }

                            break;
                        case EdmType.Boolean:
                            prop.SetValue(this, entity.BooleanValue);
                            break;
                        case EdmType.DateTime:
                            if (entity.DateTime.HasValue)
                            {
                                if (prop.PropertyType == typeof(DateTimeOffset?))
                                {
                                    prop.SetValue(this, new DateTimeOffset(entity.DateTime.Value));
                                }
                                else if (prop.PropertyType == typeof(DateTimeOffset))
                                {
                                    prop.SetValue(this, new DateTimeOffset(entity.DateTime.Value));
                                }
                                else
                                {
                                    prop.SetValue(this, entity.DateTime);
                                }
                            }

                            break;
                        case EdmType.Double:
                            prop.SetValue(this, entity.DoubleValue);
                            break;
                        case EdmType.Guid:
                            prop.SetValue(this, entity.GuidValue);
                            break;
                        case EdmType.Int32:
                            prop.SetValue(this, entity.Int32Value);
                            break;
                        case EdmType.Int64:
                            prop.SetValue(this, entity.Int64Value);
                            break;
                        default:
                            prop.SetValue(this, JsonSerializer.Deserialize(entity.StringValue, prop.PropertyType));
                            break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result = new Dictionary<string, EntityProperty>();
            foreach (var prop in GetType().GetRuntimeProperties().Where(p => p.CanWrite && !Excluded.Contains(p.Name)))
            {
                var name = prop.Name;
                var val = prop.GetValue(this);
                if (prop.GetCustomAttribute<IgnorePropertyAttribute>() != null)
                {
                    continue;
                }

                if (prop.GetCustomAttribute<BooleanColumns>() != null &&
                    typeof(IEnumerable<string>).IsAssignableFrom(prop.PropertyType))
                {
                    var set = (IEnumerable<string>)prop.GetValue(this);
                    foreach (var setval in set)
                    {
                        result.Add($"{name}_{EscapeColumnName(setval)}", EntityProperty.GeneratePropertyForBool(true));
                    }

                    continue;
                }

                switch (val)
                {
                    case string str:
                        result.Add(name, EntityProperty.GeneratePropertyForString(str));
                        break;
                    case bool bl:
                        result.Add(name, EntityProperty.GeneratePropertyForBool(bl));
                        break;
                    case DateTimeOffset dt:
                        result.Add(name, EntityProperty.GeneratePropertyForDateTimeOffset(dt));
                        break;
                    case DateTime dt:
                        result.Add(name, EntityProperty.GeneratePropertyForDateTimeOffset(new DateTimeOffset(dt)));
                        break;
                    case float db:
                        result.Add(name, EntityProperty.GeneratePropertyForDouble(db));
                        break;
                    case double db:
                        result.Add(name, EntityProperty.GeneratePropertyForDouble(db));
                        break;
                    case Guid gd:
                        result.Add(name, EntityProperty.GeneratePropertyForGuid(gd));
                        break;
                    case int it:
                        result.Add(name, EntityProperty.GeneratePropertyForInt(it));
                        break;
                    case short it:
                        result.Add(name, EntityProperty.GeneratePropertyForInt(it));
                        break;
                    case long lg:
                        result.Add(name, EntityProperty.GeneratePropertyForLong(lg));
                        break;
                    case null:
                        break;
                    default:
                        result.Add(
                            name,
                            EntityProperty.GeneratePropertyForString(
                                val?.GetType().IsEnum == true ? GetEnumValue(val) : JsonSerializer.Serialize(val)));
                        break;
                }
            }

            return result;
        }

        private string EscapeColumnName(string setval)
        {
            return setval;
        }

        private string GetEnumValue(object val)
        {
            var type = val.GetType();
            var name = val.ToString();
            var memInfo = type.GetRuntimeField(name);
            var attribute = memInfo.GetCustomAttribute<EnumMemberAttribute>();
            return attribute?.Value ?? name;
        }
    }
}