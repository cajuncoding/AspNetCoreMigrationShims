using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace AspNetCoreMigrationShims.NewtonsoftJson.NetFrameworkCompatibility
{
    /// <summary>
    /// This is a Json Converter that restores legacy ASP.Net MVC compatible behavior for handling values that cannot be assigned Null.
    /// Historically (before .NET Core) Json values of null would result in errors when setting them into values that cannot be assigned null,
    ///     however these errors treated as warnings and were skipped, leaving the original default values set on the Model. Now in 
    ///		Asp .NET Core, these failures result in Exceptions being thrown.
    /// While previously the result was that these fields were simply left their default values; either the default of the Value type 
    ///		or the default set in the Property Initializer.
    /// Therefore this Json Converter restores this behavior by assigning the Default value safely without any errors being thrown.
    /// More Info can be found here on Stack Overflow:
    ///     https://stackoverflow.com/q/71588334/7293142
    /// </summary>
    public class JsonNullToDefaultConverter : JsonConverter
    {
        //Keep an internal reference to the StringType for performance!
        private static readonly Type StringType = typeof(string);

        public static bool CanTypeBeAssignedNull(Type type)
            => !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);

        //ONLY Handle Fields that would fail a Null assignment and requires resolving of a non-null existing/default value for the Type!
        public override bool CanConvert(Type objectType)
            => !CanTypeBeAssignedNull(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingOrDefaultValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            return token.Type switch
            {
                //If the Json Token is Null then we should always use the Default Value (e.g. int=0, bool=false, etc.);
                //  also works as expected for Nullable Values (e.g. int?=null, bool?=null, etc.)
                JTokenType.Null => existingOrDefaultValue,

                //For performance we try skip exceptions for some cases:
                //  1) Cases where Empty Strings are passed (incorrectly) but previously were coerced to Default values exceptions!
                JTokenType.String when objectType != StringType && string.IsNullOrEmpty(token.Value<string>()) => existingOrDefaultValue,
                
                //Finally we allow the Token to manage the conversion of the actual value, of which exceptions
                //  will be handled in a way compatible with legacy JsonMediaTypeFormatter...
                _ => token.ToObject(objectType)
            };
        }

        // Return false; we want normal Json.NET behavior when serializing...
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) 
            => throw new NotImplementedException();
    }
}
