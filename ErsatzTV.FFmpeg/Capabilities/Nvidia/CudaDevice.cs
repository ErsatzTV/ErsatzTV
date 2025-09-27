using Lennox.NvEncSharp;

namespace ErsatzTV.FFmpeg.Capabilities.Nvidia;

public record CudaDevice(
    int Handle,
    string Model,
    Version Version,
    IReadOnlyList<CudaCodec> Encoders,
    IReadOnlyList<CudaDecoder> Decoders);

public record CudaCodec(
    string Name,
    Guid CodecGuid,
    IReadOnlyList<Guid> ProfileGuids,
    IReadOnlyList<int> BitDepths,
    bool BFrames);

public record CudaDecoder(string Name, CuVideoCodec VideoCodec, int BitDepth);
