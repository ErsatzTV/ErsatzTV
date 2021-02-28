using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Tests.Fakes
{
    public class FakeMovieWithPath : Movie
    {
        public FakeMovieWithPath(string path)
        {
            Path = path;

            MediaVersions = new List<MediaVersion>
            {
                new()
                {
                    MediaFiles = new List<MediaFile>
                    {
                        new() { Path = path }
                    }
                }
            };
        }

        public string Path { get; }
    }
}
