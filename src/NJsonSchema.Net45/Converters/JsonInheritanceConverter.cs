﻿//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Converters
{
    // IMPORTANT: Always sync with JsonInheritanceConverterTemplate.tt

    /// <summary>Defines the class as inheritance base class and adds a discriminator property to the serialized object.</summary>
    public class JsonInheritanceConverter : JsonConverter
    {
        internal static readonly string DefaultDiscriminatorName = "discriminator";

        private readonly string _discriminator;
        
        [ThreadStatic]
        private static bool _isReading;

        [ThreadStatic]
        private static bool _isWriting;

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        public JsonInheritanceConverter()
        {
            _discriminator = DefaultDiscriminatorName;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceConverter"/> class.</summary>
        /// <param name="discriminator">The discriminator.</param>
        public JsonInheritanceConverter(string discriminator)
        {
            _discriminator = discriminator;
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                _isWriting = true;

                var jObject = JObject.FromObject(value, serializer);
                jObject.AddFirst(new JProperty(_discriminator, value.GetType().Name));
                writer.WriteToken(jObject.CreateReader());
            }
            finally
            {
                _isWriting = false;
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.</summary>
        public override bool CanWrite
        {
            get
            {
                if (_isWriting)
                {
                    _isWriting = false;
                    return false; 
                }
                return true;
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.</summary>
        public override bool CanRead
        {
            get
            {
                if (_isReading)
                {
                    _isReading = false;
                    return false;
                }
                return true;
            }
        }

        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JObject>(reader);
            var discriminator = jObject.GetValue(_discriminator).Value<string>();
            var subtype = GetObjectSubtype(objectType, discriminator);

            try
            {
                _isReading = true;
                return serializer.Deserialize(jObject.CreateReader(), subtype);
            }
            finally
            {
                _isReading = false;
            }
        }

        private Type GetObjectSubtype(Type objectType, string discriminator)
        {
            var knownTypeAttributes = objectType.GetTypeInfo().GetCustomAttributes().Where(a => a.GetType().Name == "KnownTypeAttribute");
            dynamic knownTypeAttribute = knownTypeAttributes.SingleOrDefault(a => IsKnownTypeTargetType(a, discriminator));
            if (knownTypeAttribute != null)
                return knownTypeAttribute.Type;

            var typeName = objectType.Namespace + "." + discriminator;
            return objectType.GetTypeInfo().Assembly.GetType(typeName);
        }

        private bool IsKnownTypeTargetType(dynamic attribute, string discriminator)
        {
            return attribute?.Type.Name == discriminator;
        }
    }
}
