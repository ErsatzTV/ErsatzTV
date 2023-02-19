namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinPlaybackInfoResponse
{
    public IList<JellyfinMediaSourceResponse> MediaSources { get; set; }
}


// {
//     "MediaSources": [
//         {
//             "Protocol": "File",
//             "Id": "97e9ee9f29ebff3f5c7a52cbc820a3f3",
//             "Path": "\\\\whatever\\party\\central\\One Tree Hill (2003)\\Season 03\\One Tree Hill - s03e11.mkv",
//             "Type": "Default",
//             "Container": "mkv,webm",
//             "Size": 992436800,
//             "Name": "One Tree Hill - s03e11",
//             "IsRemote": false,
//             "ETag": "7b3cc3425da0b1ea7995a3cec137df48",
//             "RunTimeTicks": 24505999360,
//             "ReadAtNativeFramerate": false,
//             "IgnoreDts": false,
//             "IgnoreIndex": false,
//             "GenPtsInput": false,
//             "SupportsTranscoding": true,
//             "SupportsDirectStream": true,
//             "SupportsDirectPlay": true,
//             "IsInfiniteStream": false,
//             "RequiresOpening": false,
//             "RequiresClosing": false,
//             "RequiresLooping": false,
//             "SupportsProbing": true,
//             "VideoType": "VideoFile",
//             "MediaStreams": [
//                 {
//                     "Codec": "mpeg2video",
//                     "Language": "eng",
//                     "TimeBase": "1/1000",
//                     "CodecTimeBase": "1/25",
//                     "VideoRange": "SDR",
//                     "VideoRangeType": "SDR",
//                     "DisplayTitle": "576p MPEG2VIDEO SDR",
//                     "IsInterlaced": false,
//                     "BitRate": 3239816,
//                     "RefFrames": 1,
//                     "IsDefault": false,
//                     "IsForced": false,
//                     "Height": 576,
//                     "Width": 720,
//                     "AverageFrameRate": 25,
//                     "RealFrameRate": 25,
//                     "Profile": "Main",
//                     "Type": "Video",
//                     "AspectRatio": "16:9",
//                     "Index": 0,
//                     "IsExternal": false,
//                     "IsTextSubtitleStream": false,
//                     "SupportsExternalStream": false,
//                     "PixelFormat": "yuv420p",
//                     "Level": 8
//                 },
//                 {
//                     "Codec": "ac3",
//                     "Language": "eng",
//                     "TimeBase": "1/1000",
//                     "CodecTimeBase": "1/48000",
//                     "Title": "Stereo",
//                     "DisplayTitle": "Stereo - Eng - Dolby Digital - Default",
//                     "IsInterlaced": false,
//                     "ChannelLayout": "stereo",
//                     "BitRate": 192000,
//                     "Channels": 2,
//                     "SampleRate": 48000,
//                     "IsDefault": true,
//                     "IsForced": false,
//                     "Type": "Audio",
//                     "Index": 1,
//                     "IsExternal": false,
//                     "IsTextSubtitleStream": false,
//                     "SupportsExternalStream": false,
//                     "Level": 0
//                 },
//                 {
//                     "Codec": "DVDSUB",
//                     "Language": "eng",
//                     "TimeBase": "1/1000",
//                     "CodecTimeBase": "0/1",
//                     "LocalizedUndefined": "Undefined",
//                     "LocalizedDefault": "Default",
//                     "LocalizedForced": "Forced",
//                     "LocalizedExternal": "External",
//                     "DisplayTitle": "Eng - Default - DVDSUB",
//                     "IsInterlaced": false,
//                     "IsDefault": true,
//                     "IsForced": false,
//                     "Type": "Subtitle",
//                     "Index": 2,
//                     "Score": 112111,
//                     "IsExternal": false,
//                     "IsTextSubtitleStream": false,
//                     "SupportsExternalStream": false,
//                     "Level": 0
//                 }
//             ],
//             "MediaAttachments": [],
//             "Formats": [],
//             "Bitrate": 3431816,
//             "RequiredHttpHeaders": {},
//             "DefaultAudioStreamIndex": 1,
//             "DefaultSubtitleStreamIndex": 2
//         }
//     ],
//     "PlaySessionId": "0f085cb7bae34941ab1245b6ea799ffd"
// }
