using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Television
{
    internal static class Mapper
    {
        internal static TelevisionShowViewModel ProjectToViewModel(TelevisionShow show) =>
            new(show.Metadata.Title, show.Metadata.Year?.ToString(), show.Metadata.Plot, show.Poster);
    }
}
