using System;
using Microsoft.Net.Http.Headers;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public class JsonMediaTypes
    {
        public const string ApplicationJson = "application/json";
        public const string TextJson = "text/json";
        public const string ApplicationAnyJsonSyntax = "application/*+json";
        //public const string ApplicationJsonPatch = "application/json-patch+json";

        public static MediaTypeHeaderValue GetHeaderValue(string mediaType)
            => MediaTypeHeaderValue.Parse(mediaType).CopyAsReadOnly();
    }
}
