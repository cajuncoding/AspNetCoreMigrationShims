using Newtonsoft.Json;
using System;
using System.Buffers;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    public class JsonArrayPoolAdapter : IArrayPool<char>
    {
        protected ArrayPool<char> CharArrayPool { get; }
        
        public JsonArrayPoolAdapter(ArrayPool<char> charArrayPool)
        {
            ArgumentNullException.ThrowIfNull(charArrayPool);
            CharArrayPool = charArrayPool;
        }

        public char[] Rent(int minimumLength) 
            => CharArrayPool.Rent(minimumLength);

        public void Return(char[]? array)
        {
            if(array!= null)
                CharArrayPool.Return(array);
        }
    }
}
