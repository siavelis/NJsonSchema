//-----------------------------------------------------------------------
// <copyright file="ObjectTypeMapper.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>Maps .NET type to a generated JSON Schema describing an object.</summary>
    public class ObjectTypeMapper : ITypeMapper
    {
        private readonly Func<JsonSchemaGenerator, JsonSchemaResolver, JsonSchema4> _schemaFactory;

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schema">The schema.</param>
        public ObjectTypeMapper(Type mappedType, JsonSchema4 schema)
            : this(mappedType, (schemaGenerator, schemaResolver) => schema)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schemaFactory">The schema factory.</param>
        public ObjectTypeMapper(Type mappedType, Func<JsonSchemaGenerator, JsonSchemaResolver, JsonSchema4> schemaFactory)
        {
            _schemaFactory = schemaFactory;
            MappedType = mappedType;
        }

        /// <summary>Gets the mapped type.</summary>
        public Type MappedType { get; }

        /// <summary>Gets a value indicating whether to use a JSON Schema reference for the type.</summary>
        public bool UseReference { get; } = true;

        /// <summary>Gets the schema for the mapped type.</summary>
        /// <typeparam name="TSchemaType">The type of the schema type.</typeparam>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaGenerator">The schema generator.</param>
        /// <param name="schemaResolver">The schema resolver.</param>
#pragma warning disable 1998
        public async Task GenerateSchemaAsync<TSchemaType>(TSchemaType schema, JsonSchemaGenerator schemaGenerator, JsonSchemaResolver schemaResolver) where TSchemaType : JsonSchema4, new()
#pragma warning restore 1998
        {
            if (!schemaResolver.HasSchema(MappedType, false))
                schemaResolver.AddSchema(MappedType, false, _schemaFactory(schemaGenerator, schemaResolver));

            schema.SchemaReference = schemaResolver.GetSchema(MappedType, false);
        }
    }
}