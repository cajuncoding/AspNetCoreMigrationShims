using System;
using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public class NetFrameworkMvcNewtonsoftJsonCompatibilityPostConfigure : IPostConfigureOptions<MvcOptions>
    {
        protected MvcNewtonsoftJsonOptions JsonOptions { get; }
        protected ObjectPoolProvider ObjectPoolProvider { get; }
        protected ArrayPool<char> CharArrayPool { get; }

        public NetFrameworkMvcNewtonsoftJsonCompatibilityPostConfigure(
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions, 
            ArrayPool<char> charArrayPool,
            ObjectPoolProvider objectPoolProvider
        )
        {
            JsonOptions = jsonOptions.Value;
            ObjectPoolProvider = objectPoolProvider;
            CharArrayPool = charArrayPool;
        }

        public void PostConfigure(string name, MvcOptions mvcOptions)
        {
            //NOTE: Newtonsoft does not provide true Async processing though it does process the Stream efficiently we must buffer the stream
            //      for the most efficient processing (which prevents us from having to buffer to a string and add more Garbage Collection Pressure, etc.)
            if (mvcOptions.SuppressInputFormatterBuffering)
                throw new ArgumentOutOfRangeException(nameof(mvcOptions.SuppressInputFormatterBuffering),
                    $"Input Formatter Buffering (e.g. MvcOptions.SuppressInputFormatterBuffering) must be enabled to support optimal processing by Newtonsoft.Json Serializers.");

            var jsonSerializerSettings = JsonOptions.EnableNetFrameworkCompatibility().SerializerSettings;
            var serializerSettingsPool = ObjectPoolProvider.Create(new JsonSerializerObjectPoolPolicy(jsonSerializerSettings));

            //Configure MVC Input Formatters with dependencies...
            var mvcInputFormatters = mvcOptions.InputFormatters;
            mvcInputFormatters.RemoveType<NewtonsoftJsonInputFormatter>();
            //NOTE: We can't Insert at the Beginning because (as noted in the NewtonsoftJson comments) the Patch Formatter
            //          must run first otherwise the Json Input Formatter will handle it's cases!
            mvcInputFormatters.Add(new NetFrameworkMvcCompatibleNewtonsoftJsonInputFormatter(
                JsonOptions,
                serializerSettingsPool,
                CharArrayPool
            ));
        }
    }
}
