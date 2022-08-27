using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public static class MvcConfigurationExtensions
    {
        public static IMvcBuilder AddNewtonsoftJsonWithNetFrameworkCompatibility(this IMvcBuilder mvcBuilder, Action<MvcNewtonsoftJsonOptions>? setupAction = null)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder);

            if (setupAction == null)
                mvcBuilder.AddNewtonsoftJson();
            else
                mvcBuilder.AddNewtonsoftJson(setupAction);

            return mvcBuilder.WithNewtonsoftJsonNetFrameworkCompatibility();
        }

        public static IMvcBuilder WithNewtonsoftJsonNetFrameworkCompatibility(this IMvcBuilder mvcBuilder)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder);

            //var services = mvcBuilder.Services;
            mvcBuilder.Services.AddSingleton<IPostConfigureOptions<MvcOptions>, NetFrameworkMvcNewtonsoftJsonCompatibilityPostConfigure>();

            return mvcBuilder;
        }

        /// <summary>
        /// Configures the specified JsonSerializerSettings for .NET Framework MVC Compatibility (e.g. adds NullToDefaultJsonConverter, etc.)
        /// </summary>
        /// <param name="jsonOptions"></param>
        /// <returns></returns>
        public static MvcNewtonsoftJsonOptions EnableNetFrameworkCompatibility(this MvcNewtonsoftJsonOptions jsonOptions)
        {
            var serializerSettings = jsonOptions.SerializerSettings;

            //Enable Compatibility Converters...
            var jsonConverters = serializerSettings.Converters;
            if (!jsonConverters.Any(c => c is JsonNullToDefaultConverter))
                jsonConverters.Add(new JsonNullToDefaultConverter());

            //Enable Culture on the Settings...
            if (jsonOptions.ReadJsonWithRequestCulture)
                serializerSettings.Culture = CultureInfo.CurrentCulture;

            return jsonOptions;
        }
    }
}
