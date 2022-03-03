using System.Text;
using ErsatzTV.Core.FFmpeg;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace ErsatzTV.Formatters;

public class ConcatPlaylistOutputFormatter : TextOutputFormatter
{
    public ConcatPlaylistOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type type) => typeof(ConcatPlaylist).IsAssignableFrom(type);

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) =>
        // ReSharper disable once PossibleNullReferenceException
        context.HttpContext.Response.WriteAsync(((ConcatPlaylist) context.Object).ToString());
}