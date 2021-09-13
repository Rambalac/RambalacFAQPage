// <copyright file="DictionaryTableEntity.cs" company="T-Rnd">
// Copyright (c) T-Rnd. All rights reserved.
// </copyright>

namespace DataStorage.Azure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Cosmos.Table;

    /// <inheritdoc />
    /// <summary>
    /// Entity with dynamic number of fields.
    /// </summary>
    public class DictionaryTableEntity<T> : TableEntity where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryTableEntity" /> class.
        /// </summary>

        protected DictionaryTableEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryTableEntity" /> class.
        /// </summary>
        /// <param name="partitionKey">Partition Key.</param>
        /// <param name="rowKey">Row Key.</param>
        protected DictionaryTableEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryTableEntity" /> class.
        /// </summary>
        /// <param name="partitionKey">Partition Key.</param>
        /// <param name="rowKey">Row Key.</param>
        /// <param name="fields">Field to pre-initialize.</param>
        protected DictionaryTableEntity(string partitionKey, string rowKey, Dictionary<string, T> fields)
            : base(partitionKey, rowKey)
        {
            Fields = fields;
        }

        /// <summary>
        /// Gets fields dictionary.
        /// </summary>

        public Dictionary<string, T> Fields { get; } = new Dictionary<string, T>();

        /// <inheritdoc />
        public override void ReadEntity(
             IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            foreach (var prop in properties)
            {
                object val;
                switch (prop.Value.PropertyType)
                {
                    case EdmType.Binary:
                        val = prop.Value.BinaryValue;
                        break;
                    case EdmType.String:
                        val = prop.Value.StringValue;
                        break;
                    case EdmType.Boolean:
                        val = prop.Value.BooleanValue;
                        break;
                    case EdmType.DateTime:
                        val = prop.Value.DateTime;
                        break;
                    case EdmType.Double:
                        val = prop.Value.DoubleValue;
                        break;
                    case EdmType.Guid:
                        val = prop.Value.GuidValue;
                        break;
                    case EdmType.Int32:
                        val = prop.Value.Int32Value;
                        break;
                    case EdmType.Int64:
                        val = prop.Value.Int64Value;
                        break;
                    default:
                        throw new NotSupportedException("Field is not supported: " + prop.Value.PropertyType);
                }

                Fields[prop.Key] = (T)val;
            }
        }

        /// <inheritdoc />
        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result = new Dictionary<string, EntityProperty>();
            foreach (var prop in Fields)
            {
                var name = prop.Key;
                var val = prop.Value;
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
                        throw new NotSupportedException("Field is not supported: " + val.GetType());
                }
            }

            return result;
        }
    }
}