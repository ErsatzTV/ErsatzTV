namespace ErsatzTV.Core.FFmpeg;

public interface IHlsInitSegmentCache
{
    Task AddSegment(string segment);

    string EarliestSegmentByHash(long generatedAt);

    bool IsEarliestByHash(string fileName);

    void DeleteSegment(string segment);
}
