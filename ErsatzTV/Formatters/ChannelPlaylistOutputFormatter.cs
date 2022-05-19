using System.Text;
using ErsatzTV.Core.Iptv;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ErsatzTV.Formatters;

public class ChannelPlaylistOutputFormatter : TextOutputFormatter
{
    public ChannelPlaylistOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-mpegurl"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type type) => typeof(ChannelPlaylist).IsAssignableFrom(type);

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
        // ReSharper disable once PossibleNullReferenceException
        context.HttpContext.Response.WriteAsync(((ChannelPlaylist)context.Object).ToM3U());
}
