using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Metadata.Nfo;

namespace ErsatzTV.Core.Interfaces.Metadata.Nfo;

public interface IEpisodeNfoReader
{
    Task<List<TvShowEpisodeNfo>> Read(Stream input);
}