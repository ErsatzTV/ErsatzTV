namespace ErsatzTV.Application.Streaming;

public enum HlsSessionState
{
    SeekAndWorkAhead,
    ZeroAndWorkAhead,
    SeekAndRealtime,
    ZeroAndRealtime,
    PlayoutUpdated
}
