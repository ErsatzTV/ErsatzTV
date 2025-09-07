using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public abstract record WatermarkResult;

public sealed record InheritWatermark : WatermarkResult;

public sealed record DisableWatermark : WatermarkResult;

public sealed record CustomWatermarks(List<ChannelWatermark> Watermarks) : WatermarkResult;
