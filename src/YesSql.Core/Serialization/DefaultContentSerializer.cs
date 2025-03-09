using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YesSql.Serialization
{
    public class DefaultContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerOptions _options;

        public DefaultContentSerializer()
        {
            _options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _options.Converters.Add(UtcDateTimeJsonConverter.Instance);
            _options.Converters.Add(DynamicJsonConverter.Instance);
        }

        public DefaultContentSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public object Deserialize(string content, Type type, Func<string, long, object> objectResolver) =>
            JsonSerializer.Deserialize(
                content,
                type,
                CopyOptionsAppendRelationConverter(objectResolver: objectResolver));

        public string Serialize(object item, Func<string, object, long> idResolver) =>
            JsonSerializer.Serialize(
                item,
                CopyOptionsAppendRelationConverter(idResolver: idResolver));

        private JsonSerializerOptions CopyOptionsAppendRelationConverter(
            Func<string, object, long> idResolver = null,
            Func<string, long, object> objectResolver = null)
        {
            var copy = new JsonSerializerOptions
            {
                AllowTrailingCommas = _options.AllowTrailingCommas,
                DefaultBufferSize = _options.DefaultBufferSize,
                DefaultIgnoreCondition = _options.DefaultIgnoreCondition,
                DictionaryKeyPolicy = _options.DictionaryKeyPolicy,
                Encoder = _options.Encoder,
                IgnoreReadOnlyFields = _options.IgnoreReadOnlyFields,
                IgnoreReadOnlyProperties = _options.IgnoreReadOnlyProperties,
                IncludeFields = _options.IncludeFields,
                MaxDepth = _options.MaxDepth,
                NumberHandling = _options.NumberHandling,
                PropertyNameCaseInsensitive = _options.PropertyNameCaseInsensitive,
                PropertyNamingPolicy = _options.PropertyNamingPolicy,

                ReadCommentHandling = _options.ReadCommentHandling,
                WriteIndented = _options.WriteIndented,
            };

            foreach (var converter in _options.Converters)
            {
                copy.Converters.Add(converter);
            }

            copy.Converters.Add(new RelationContainerConverter(idResolver, objectResolver));

            return copy;
        }
    }
}
