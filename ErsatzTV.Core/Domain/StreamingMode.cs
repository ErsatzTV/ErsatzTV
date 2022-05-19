namespace ErsatzTV.Core.Domain;

public enum StreamingMode
{
    TransportStream = 1,
    HttpLiveStreamingDirect = 2,

//        HttpLiveStreamingHybrid = 3,
    HttpLiveStreamingSegmenter = 4,
    TransportStreamHybrid = 5
}
