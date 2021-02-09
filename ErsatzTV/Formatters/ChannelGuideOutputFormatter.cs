using System;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Iptv;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ErsatzTV.Formatters
{
    public class ChannelGuideOutputFormatter : TextOutputFormatter
    {
        public ChannelGuideOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type) => typeof(ChannelGuide).IsAssignableFrom(type);

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
            // ReSharper disable once PossibleNullReferenceException
            context.HttpContext.Response.WriteAsync(((ChannelGuide) context.Object).ToXml());
    }
}
