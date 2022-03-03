using System.Text;
using ErsatzTV.Core.Hdhr;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ErsatzTV.Formatters;

public class HdhrJsonOutputFormatter : TextOutputFormatter
{
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    public HdhrJsonOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);

        _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            }
        };
    }

    protected override bool CanWriteType(Type type) =>
        typeof(Discover).IsAssignableFrom(type) || typeof(LineupStatus).IsAssignableFrom(type) ||
        typeof(IEnumerable<LineupItem>).IsAssignableFrom(type);

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
        // ReSharper disable once PossibleNullReferenceException
        context.HttpContext.Response.WriteAsync(
            JsonConvert.SerializeObject(context.Object, _jsonSerializerSettings));
}