using System;
using System.Text;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Iptv;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
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

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var httpContext = context.HttpContext;
            var servicesProvider = httpContext.RequestServices;

            var maybeJellyfin = Option<JellyfinMediaSource>.None;
            var maybeEmby = Option<EmbyMediaSource>.None;
            
            var mediaSourceRepository = servicesProvider.GetService<IMediaSourceRepository>();
            if (mediaSourceRepository != null)
            {
                maybeJellyfin = await mediaSourceRepository.GetAllJellyfin()
                    .Map(list => list.HeadOrNone());

                maybeEmby = await mediaSourceRepository.GetAllEmby()
                    .Map(list => list.HeadOrNone());
            }

            // ReSharper disable once PossibleNullReferenceException
            await context.HttpContext.Response.WriteAsync(
                ((ChannelGuide) context.Object).ToXml(maybeJellyfin, maybeEmby));
        }
    }
}
