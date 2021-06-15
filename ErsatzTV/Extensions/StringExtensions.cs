using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace ErsatzTV.Extensions
{
    public static class StringExtensions
    {
        public static string GetSearchQuery(this string uri)
        {
            try
            {
                string query = new Uri(uri).Query;
                Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(query);
                if (parsed.TryGetValue("query", out StringValues value))
                {
                    return value;
                }

                if (parsed.TryGetValue("b64query", out StringValues base64Value))
                {
                    return base64Value.DecodeBase64();
                }
            }
            catch (Exception)
            {
                // do nothing
            }

            return string.Empty;
        }

        public static string GetRelativeSearchQuery(this string query)
        {
            (string key, string value) = EncodeQuery(query);
            return $"/search?{key}={value}";
        }

        private static string DecodeBase64(this StringValues input) =>
            Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input));

        public static EncodedQueryResult EncodeQuery(this string query)
        {
            string encoded = Uri.EscapeDataString(query);

            // TODO: remove this on dotnet 6
            // see https://github.com/dotnet/aspnetcore/pull/26769
            var fakeAbsolute = $"https://whatever.com/test?query={encoded}";
            return Uri.IsWellFormedUriString(fakeAbsolute, UriKind.Absolute)
                ? new EncodedQueryResult("query", encoded)
                : new EncodedQueryResult("b64query", WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(query)));
        }

        public record EncodedQueryResult(string Key, string Value);
    }
}
