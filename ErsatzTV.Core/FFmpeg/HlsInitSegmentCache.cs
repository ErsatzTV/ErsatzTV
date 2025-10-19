using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Core.FFmpeg;

public class HlsInitSegmentCache(ILocalFileSystem localFileSystem) : IHlsInitSegmentCache
{
    private readonly ConcurrentDictionary<string, string> _knownSegments = [];
    private readonly ConcurrentDictionary<string, string> _earliestSegmentsByHash = [];

    public async Task AddSegment(string segment)
    {
        string fileName = Path.GetFileName(segment);
        if (!_knownSegments.ContainsKey(fileName))
        {
            byte[] hash = await localFileSystem.GetHash(segment);
            string hashString = Convert.ToHexStringLower(hash);

            //logger.LogDebug("Adding segment {Segment} to cache", fileName);

            _knownSegments.TryAdd(fileName, hashString);

            if (!_earliestSegmentsByHash.TryAdd(hashString, fileName))
            {
                //logger.LogDebug("An earlier segment with the same hash was already in the cache");
            }
        }
    }

    public string EarliestSegmentByHash(long generatedAt)
    {
        var fileName = $"{generatedAt}_init.mp4";
        string hashString = _knownSegments[fileName];
        return _earliestSegmentsByHash[hashString];
    }

    public bool IsEarliestByHash(string fileName)
    {
        string hashString = _knownSegments[fileName];
        return _earliestSegmentsByHash[hashString] == fileName;
    }

    public void DeleteSegment(string segment)
    {
        //logger.LogDebug("Segment {Segment} is no longer needed; deleting cached hash", segment);
        _knownSegments.TryRemove(segment, out _);
    }
}
