using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Configuration
{
    internal static class Mapper
    {
        internal static ConfigElementViewModel ProjectToViewModel(ConfigElement element) =>
            new(element.Key, element.Value);
    }
}
