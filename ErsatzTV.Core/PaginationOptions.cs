using System;

namespace ErsatzTV.Core;

public static class PaginationOptions
{
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 5000;

    public static int NormalizePageSize(int? requested) =>
        NormalizePageSize(requested, DefaultPageSize, MaxPageSize);

    public static int NormalizePageSize(int? requested, int defaultSize, int maxSize)
    {
        if (requested is null || requested <= 0)
        {
            return defaultSize;
        }

        return Math.Clamp(requested.Value, 1, maxSize);
    }
}
