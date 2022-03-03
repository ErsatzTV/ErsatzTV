using System.Text;
using ErsatzTV.Core.Hdhr;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ErsatzTV.Formatters;

public class DeviceXmlOutputFormatter : TextOutputFormatter
{
    public DeviceXmlOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type type) => typeof(DeviceXml).IsAssignableFrom(type);

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
        // ReSharper disable once PossibleNullReferenceException
        context.HttpContext.Response.WriteAsync(((DeviceXml) context.Object).ToXml());
}