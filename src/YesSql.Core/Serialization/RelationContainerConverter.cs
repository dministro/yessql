using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YesSql.Attributes;
using YesSql.Extensions;

namespace YesSql.Serialization
{
    public class RelationContainerConverter : JsonConverter<object>
    {
        private readonly Func<string, object, long> _idResolver;
        private readonly Func<string, long, object> _objectResolver;

        public RelationContainerConverter(Func<string, object, long> idResolver, Func<string, long, object> objectResolver) =>
            (_idResolver, _objectResolver) = (idResolver, objectResolver);

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.HasAttribute<RelationContainerAttribute>();

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            var item = Activator.CreateInstance(typeToConvert);

            foreach (var property in typeToConvert.GetProperties())
            {
                var jsonIgnoreAttribute = property.GetCustomAttribute<JsonIgnoreAttribute>(true);
                if (jsonIgnoreAttribute is not null)
                {
                    continue;
                }

                if (!(property.CanWrite && property.SetMethod.IsPublic))
                {
                    continue;
                }

                var referenceAttribute = property.GetCustomAttribute<ReferencedPropertyAttribute>(false);
                if (referenceAttribute is null)
                {
                    if (!jsonDictionary.TryGetValue(ConvertPropertyName(options.PropertyNamingPolicy, property.Name), out var propertyValue))
                    {
                        continue;
                    }

                    property.SetValue(item, JsonSerializer.Deserialize(propertyValue, property.PropertyType, options));

                    continue;
                }

                if (!jsonDictionary.TryGetValue(ConvertReferencePropertyName(options.PropertyNamingPolicy, property.Name), out var referencedValue))
                {
                    property.SetValue(item, null);

                    continue;
                }

                if (referencedValue.ValueKind == JsonValueKind.Array)
                {
                    ReadReferencedListLikePropertyValue(referencedValue, property, referenceAttribute.Collection, item, options);
                }
                else if (referencedValue.ValueKind == JsonValueKind.Number)
                {
                    ReadReferencedPropertyValue(referencedValue, property, referenceAttribute.Collection, item, options);
                }
                else if (referencedValue.ValueKind == JsonValueKind.Null)
                {
                    property.SetValue(item, null);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            return item;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var property in value.GetType().GetProperties())
            {
                var jsonIgnoreAttribute = property.GetCustomAttribute<JsonIgnoreAttribute>(true);
                if (jsonIgnoreAttribute is not null)
                {
                    continue;
                }

                if (!(property.CanWrite && property.SetMethod.IsPublic) && options.IgnoreReadOnlyProperties)
                {
                    continue;
                }

                var propertyValue = property.GetValue(value);
                if (propertyValue is null && options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                {
                    continue;
                }

                var referenceAttribute = property.GetCustomAttribute<ReferencedPropertyAttribute>(false);
                if (referenceAttribute is null)
                {
                    writer.WritePropertyName(ConvertPropertyName(options.PropertyNamingPolicy, property.Name));
                    JsonSerializer.Serialize(writer, propertyValue, options);

                    continue;
                }

                WriteReferenceProperty(
                    writer,
                    options.PropertyNamingPolicy,
                    property,
                    referenceAttribute.Collection,
                    propertyValue);
            }

            writer.WriteEndObject();
        }

        private void ReadReferencedListLikePropertyValue(
            JsonElement element,
            PropertyInfo property,
            string collection,
            object item,
            JsonSerializerOptions options)
        {
            var ids = JsonSerializer.Deserialize<long[]>(element, options);
            var referencedItems = ids.Select(id => _objectResolver.Invoke(collection, id)).ToArray();
            if (property.PropertyType == typeof(IEnumerable<>).MakeGenericType(property.PropertyType.GenericTypeArguments))
            {
                var typedArray = Array.CreateInstance(property.PropertyType.GenericTypeArguments.Single(), referencedItems.Length);
                for (var itemIndex = 0; itemIndex < typedArray.Length; itemIndex++)
                {
                    typedArray.SetValue(referencedItems[itemIndex], itemIndex);
                }

                property.SetValue(item, typedArray);

                return;
            }

            throw new NotSupportedException();
        }

        private void ReadReferencedPropertyValue(
            JsonElement element,
            PropertyInfo property,
            string collection,
            object item,
            JsonSerializerOptions options)
        {
            var id = JsonSerializer.Deserialize<long>(element, options);
            var referencedItem = _objectResolver.Invoke(collection, id);

            property.SetValue(item, referencedItem);
        }

        private void WriteReferenceProperty(
            Utf8JsonWriter writer,
            JsonNamingPolicy namingPolicy,
            PropertyInfo property,
            string collection,
            object value)
        {
            writer.WritePropertyName(ConvertReferencePropertyName(namingPolicy, property.Name));

            if (value is null)
            {
                writer.WriteNullValue();

                return;
            }

            var valueType = value.GetType();
            if (valueType.IsAssignableFrom(typeof(IDictionary<,>)))
            {
                throw new NotSupportedException();
            }
            else if (value is IEnumerable enumerable)
            {
                var ids = enumerable
                    .OfType<object>()
                    .Select(item => _idResolver.Invoke(collection, item));

                writer.WriteStartArray();

                foreach (var id in ids)
                {
                    writer.WriteNumberValue(id);
                }

                writer.WriteEndArray();
            }
            else
            {
                var id = _idResolver.Invoke(collection, value);

                writer.WriteNumberValue(id);
            }
        }

        private static string ConvertPropertyName(JsonNamingPolicy namingPolicy, string propertyName) =>
            namingPolicy?.ConvertName(propertyName) ?? propertyName;

        private static string ConvertReferencePropertyName(JsonNamingPolicy namingPolicy, string propertyName) =>
            $"$_{ConvertPropertyName(namingPolicy, propertyName)}_ref";
    }
}
