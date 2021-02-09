using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static Unit Initialize(TvContext context)
        {
            if (context.Resolutions.Any())
            {
                return Unit.Default;
            }

            var resolutions = new List<Resolution>
            {
                new() { Id = 1, Name = "720x480", Width = 720, Height = 480 },
                new() { Id = 2, Name = "1280x720", Width = 1280, Height = 720 },
                new() { Id = 3, Name = "1920x1080", Width = 1920, Height = 1080 },
                new() { Id = 4, Name = "3840x2160", Width = 3840, Height = 2160 }
            };
            context.Resolutions.AddRange(resolutions);
            context.SaveChanges();

            var resolutionConfig = new ConfigElement
            {
                Key = ConfigElementKey.FFmpegDefaultResolutionId.Key,
                Value = "3" // 1920x1080
            };
            context.ConfigElements.Add(resolutionConfig);
            context.SaveChanges();

            var defaultProfile = FFmpegProfile.New("1920x1080 x264 ac3", resolutions[2]);
            context.FFmpegProfiles.Add(defaultProfile);
            context.SaveChanges();

            var profileConfig = new ConfigElement
            {
                Key = ConfigElementKey.FFmpegDefaultProfileId.Key,
                Value = defaultProfile.Id.ToString()
            };
            context.ConfigElements.Add(profileConfig);
            context.SaveChanges();

            var defaultChannel = new Channel(Guid.NewGuid())
            {
                Number = 1,
                Name = "ErsatzTV",
                FFmpegProfile = defaultProfile,
                StreamingMode = StreamingMode.TransportStream
            };
            context.Channels.Add(defaultChannel);
            context.SaveChanges();

            // TODO: clean this up
            // var mediaSource = new LocalMediaSource
            // {
            //     Name = "Default"
            // };
            // context.MediaSources.Add(mediaSource);
            // context.SaveChanges();

            // TODO: create looping static image that mentions configuring via web
            return Unit.Default;
        }
    }
}
