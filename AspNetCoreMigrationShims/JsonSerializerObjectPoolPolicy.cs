using Newtonsoft.Json;
using System;
using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    /// <summary>
    /// Provides a public implementation of the JsonSerializerSettings Object Pool (the default Newtonsoft implementation is internal).
    /// </summary>
    public class JsonSerializerObjectPoolPolicy : IPooledObjectPolicy<JsonSerializer>
    {
        protected  JsonSerializerSettings SerializerSettings { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonSerializerObjectPoolPolicy"/>.
        /// </summary>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> used to instantiate
        /// <see cref="JsonSerializer"/> instances.</param>
        public JsonSerializerObjectPoolPolicy(JsonSerializerSettings serializerSettings) => SerializerSettings = serializerSettings;

        /// <inheritdoc />
        public JsonSerializer Create() => JsonSerializer.Create(SerializerSettings);

        /// <inheritdoc />
        public bool Return(JsonSerializer serializer) => true;
    }
}
