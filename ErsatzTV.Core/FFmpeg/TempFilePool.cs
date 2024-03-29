﻿using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public class TempFilePool : ITempFilePool
{
    private const int ItemLimit = 10;
    private readonly object _lock = new();
    private readonly Dictionary<TempFileCategory, int> _state = new();

    public string GetNextTempFile(TempFileCategory category)
    {
        lock (_lock)
        {
            var index = 0;

            if (_state.TryGetValue(category, out int current))
            {
                index = (current + 1) % ItemLimit;
            }

            _state[category] = index;

            return GetFileName(category, index);
        }
    }

    private static string GetFileName(TempFileCategory category, int index) => Path.Combine(
        FileSystemLayout.TempFilePoolFolder,
        $"{category}_{index}".ToLowerInvariant());
}
