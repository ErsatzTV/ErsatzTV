using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

internal record DisplaySize(int Width, int Height) : IDisplaySize;
