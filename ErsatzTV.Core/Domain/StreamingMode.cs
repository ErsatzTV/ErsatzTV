namespace ErsatzTV.Core.Domain;

public enum StreamingMode
{
    TransportStream = 1,
    HttpLiveStreamingDirect = 2,

    HttpLiveStreamingSegmenter = 4,
    TransportStreamHybrid = 5,

    // HttpLiveStreamingSegmenterLegacy = 999
}
