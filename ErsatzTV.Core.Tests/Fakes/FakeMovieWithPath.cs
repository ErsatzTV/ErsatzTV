using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Tests.Fakes;

public class FakeMovieWithPath : MediaItemScanResult<Movie>
{
    public FakeMovieWithPath(string path)
        : base(
            new Movie
            {
                MediaVersions = new List<MediaVersion>
                {
                    new()
                    {
                        MediaFiles = new List<MediaFile>
                        {
                            new() { Path = path }
                        }
                    }
                }
            }) =>
        IsAdded = true;
}