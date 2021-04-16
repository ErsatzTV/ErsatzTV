namespace ErsatzTV.Application.Configuration
{
    internal static class Mapper
    {
        internal static ConfigElementViewModel ProjectToViewModel(Core.Domain.ConfigElement element) =>
            new(element.Key, element.Value);
    }
}
