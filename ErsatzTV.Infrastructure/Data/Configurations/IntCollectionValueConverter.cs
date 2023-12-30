using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class IntCollectionValueConverter : ValueConverter<ICollection<int>, string>
{
    public IntCollectionValueConverter() : base(
        i => string.Join(",", i),
        s => string.IsNullOrWhiteSpace(s)
            ? Array.Empty<int>()
            : s.Split(',', StringSplitOptions.None).Select(int.Parse).ToArray())
    {
    }
}
