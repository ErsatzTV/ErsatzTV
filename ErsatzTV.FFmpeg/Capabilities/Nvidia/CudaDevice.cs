namespace ErsatzTV.FFmpeg.Capabilities.Nvidia;

public record CudaDevice(int Handle, string Model, Version Version, IReadOnlyList<CudaCodec> Codecs);

public record CudaCodec(string Name, Guid CodecGuid, IReadOnlyList<Guid> ProfileGuids, IReadOnlyList<int> BitDepths);
