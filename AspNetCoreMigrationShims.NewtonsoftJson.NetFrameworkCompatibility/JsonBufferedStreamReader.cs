using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public class JsonBufferedHttpRequestReader : IDisposable
    {
        public const int DefaultMemoryThreshold = 1024 * 1024; //1MB

        public int MemoryThreshold { get; protected set; }

        public JsonTextReader JsonTextReader { get; protected set; } = null!;

        protected FileBufferingReadStream FileBufferingReadStream { get; set; } = null!;

        protected TextReader? StreamTextReader { get; set; }

        public static async Task<JsonBufferedHttpRequestReader> CreateAsync(
            HttpRequest httpRequest,
            Encoding effectiveEncoding,
            ArrayPool<char> charArrayPool,
            int memoryThreshold = DefaultMemoryThreshold,
            Func<Stream, Encoding, TextReader>? readerFactory = null
        )
        {
            var @this = new JsonBufferedHttpRequestReader();

            //Compute the smaller valid value of the current Content Length vs the configured Memory Threshold;
            //NOTE: It's critical for performance that in most cases the MemoryThreshold be larger than the Content Size to prevent buffering to the Filesystem!
            @this.MemoryThreshold = memoryThreshold;
            var contentLength = httpRequest.ContentLength.GetValueOrDefault();
            if (contentLength > 0 && contentLength < memoryThreshold)
                @this.MemoryThreshold = (int)contentLength;

            var streamReaderFactory = readerFactory
                ?? new Func<Stream, Encoding, TextReader>((stream, encoding) => new StreamReader(stream, encoding));

            //Initialize the buffered request so that the NewtonsoftJson deserializer can process the stream synchronously
            //  because NewtonsoftJson does not support full async reading of the stream as this ia key benefit of migrating/upgrading to System.Text.Json (unlikely to ever be implemented).
            //  Though the processing still handles the stream as efficiently as possible with use of ArrayPool<byte> (saves allocations), and chunk reading from the stream.
            //  In addition this prevents us from having to buffer to a String which is allocated and causes additional Garbage Collection pressure!
            //  NOTE: These things are important as this is the Model Binding logic run on every single request!
            var bufferingReadStream = @this.FileBufferingReadStream = new FileBufferingReadStream(httpRequest.Body, memoryThreshold);
            await bufferingReadStream.DrainAsync(CancellationToken.None);
            bufferingReadStream.Seek(0L, SeekOrigin.Begin);

            var streamReader = @this.StreamTextReader = streamReaderFactory(bufferingReadStream, effectiveEncoding);
            @this.JsonTextReader = new JsonTextReader(streamReader)
            {
                ArrayPool = new JsonCharArrayPool(charArrayPool)
            };

            return @this;
        }

        public void Dispose()
        {
            JsonTextReader?.Close();
            StreamTextReader?.Dispose();
            FileBufferingReadStream?.Dispose();
        }
    }
}
