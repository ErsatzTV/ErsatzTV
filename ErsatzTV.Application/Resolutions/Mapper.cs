using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Resolutions
{
    internal static class Mapper
    {
        internal static ResolutionViewModel ProjectToViewModel(Resolution resolution) =>
            new(resolution.Id, resolution.Name, resolution.Width, resolution.Height);
    }
}
