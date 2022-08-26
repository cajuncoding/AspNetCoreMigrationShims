using System;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using JsonErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public class NetFrameworkMvcCompatibleNewtonsoftJsonInputFormatter : TextInputFormatter
    {
        protected MvcNewtonsoftJsonOptions JsonOptions { get; }

        protected ObjectPool<JsonSerializer> JsonSerializerPool { get; }

        public NetFrameworkMvcCompatibleNewtonsoftJsonInputFormatter(
            MvcNewtonsoftJsonOptions jsonOptions,
            ObjectPool<JsonSerializer> jsonSerializerPool
        )
        {
            JsonOptions = jsonOptions;
            JsonSerializerPool = jsonSerializerPool;

            //Add Supported Encodings
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            //Add Supported Media Types
            SupportedMediaTypes.Add(JsonMediaTypes.GetHeaderValue(JsonMediaTypes.ApplicationJson));
            SupportedMediaTypes.Add(JsonMediaTypes.GetHeaderValue(JsonMediaTypes.TextJson));
            SupportedMediaTypes.Add(JsonMediaTypes.GetHeaderValue(JsonMediaTypes.ApplicationAnyJsonSyntax));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding effectiveEncoding)
        {
            var httpRequest = context.HttpContext.Request;
            var jsonSerializer = JsonSerializerPool.Get();

            //Initialize the Error Handler which is a CRITICAL for compatible behavior with legacy JsonMediaTypeFormatter behavior!
            void EventHandler(object? sender, JsonErrorEventArgs jsonErrorEventArgs) => HandleError(sender, jsonErrorEventArgs, context);
            jsonSerializer.Error += EventHandler;

            object? model;
            try
            {
                using var jsonBufferedRequestReader = await JsonBufferedHttpRequestReader.CreateAsync(
                    httpRequest, 
                    effectiveEncoding, 
                    JsonOptions.InputFormatterMemoryBufferThreshold, 
                    context.ReaderFactory
                );
                
                model = jsonSerializer.Deserialize(jsonBufferedRequestReader.JsonTextReader, context.ModelType);
            }
            finally
            {
                jsonSerializer.Error -= EventHandler;
                JsonSerializerPool.Return(jsonSerializer);
            }

            //NOTE: Logic Taken directly from NewtonsoftJsonInputFormatter
            // Some nonempty inputs might deserialize as null, for example whitespace,
            // or the JSON-encoded value "null". The upstream BodyModelBinder needs to
            // be notified that we don't regard this as a real input so it can register
            // a model binding error.
            if (model == null && !context.TreatEmptyInputAsDefaultValue)
                // ReSharper disable once MethodHasAsyncOverload
                return InputFormatterResult.NoValue();
            else
                // ReSharper disable once MethodHasAsyncOverload
                return InputFormatterResult.Success(model);
        }

        protected virtual void HandleError(object? sender, JsonErrorEventArgs errorEventArgs, InputFormatterContext context)
        {
            var errorContext = errorEventArgs.ErrorContext;

            var exception = WrapExceptionForModelState(errorContext.Error);
            var modelStateKey = CreateJsonMediaFormatterModelStateKey(errorEventArgs, context);

            //Add the Error to the Model state in as close of a way as we can to match Legacy JsonMediaFormatter behavior!
            context.ModelState.TryAddModelError(modelStateKey, exception, context.Metadata);

            // Error must always be marked as handled!
            // NOTE: This is critical to match Legacy JsonMediaFormatter behavior!
            // NOTE: Also per comments taken directly from NewtonsoftJsonInputFormatter:
            //  Failure to do so can cause the exception to be rethrown at every recursive level and overflow the stack for x64 CLR processes
            errorContext.Handled = true;
        }

        protected virtual string CreateJsonMediaFormatterModelStateKey(JsonErrorEventArgs errorEventArgs, InputFormatterContext context)
        {
            //Legacy JsonMediaFormatter concatenated the Keys as follows
            //NOTE: Logic adapted from: System.Web.Http.ModelBinding.ModelBindingHelper (prefix = parameterName, suffix = errorPath).
            var prefix = context.Metadata.ParameterName;
            var suffix = errorEventArgs.ErrorContext.Path;

            return suffix.StartsWith("[", StringComparison.Ordinal)
                ? string.Concat(prefix, suffix)
                : string.Concat(prefix, ".", suffix);
        }


        //NOTE: Logic Taken directly from NewtonsoftJsonInputFormatter
        protected virtual Exception WrapExceptionForModelState(Exception exception)
        {
            // In 2.0 and earlier we always gave a generic error message for errors that come from JSON.NET
            // We only allow it in 2.1 and newer if the app opts-in.
            if (!JsonOptions.AllowInputFormatterExceptionMessages)
            {
                // This app is not opted-in to JSON.NET messages, return the original exception.
                return exception;
            }

            // It's not known that Json.NET currently ever raises error events with exceptions
            // other than these two types, but we're being conservative and limiting which ones
            // we regard as having safe messages to expose to clients
            if (exception is JsonReaderException || exception is JsonSerializationException)
            {
                // InputFormatterException specifies that the message is safe to return to a client, it will
                // be added to model state.
                return new InputFormatterException(exception.Message, exception);
            }

            // Not a known exception type, so we're not going to assume that it's safe.
            return exception;
        }
    }
}
