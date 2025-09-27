using System.Globalization;
using System.Text;
using ErsatzTV.FFmpeg.Format;
using Lennox.NvEncSharp;

namespace ErsatzTV.FFmpeg.Capabilities.Nvidia;

internal static class CudaHelper
{
    private static bool _success;
    private static bool _initialized;
    private static readonly Lock Lock = new();

    private static readonly Dictionary<string, Guid> AllCodecs = new()
    {
        [VideoFormat.H264] = NvEncCodecGuids.H264,
        [VideoFormat.Hevc] = NvEncCodecGuids.Hevc
    };

    private static bool EnsureInit()
    {
        if (_initialized)
        {
            return _success;
        }

        lock (Lock)
        {
            if (_initialized)
            {
                return _success;
            }

            try
            {
                LibNvEnc.TryInitialize(out string? error);
                if (string.IsNullOrEmpty(error))
                {
                    LibCuda.Initialize();
                    _success = true;
                }
            }
            catch (LibNvEncException)
            {
                _success = false;
            }

            _initialized = true;
        }

        return _success;
    }

    internal static Option<List<CudaDevice>> GetDevices()
    {
        var result = new List<CudaDevice>();

        if (!EnsureInit())
        {
            return Option<List<CudaDevice>>.None;
        }

        foreach (var description in CuDevice.GetDescriptions())
        {
            var device = description.Device;

            string name = device.GetName();
            int nullIndex = name.IndexOf('\0');
            if (nullIndex > 0)
            {
                name = name[..nullIndex];
            }

            int major = device.GetAttribute(CuDeviceAttribute.ComputeCapabilityMajor);
            int minor = device.GetAttribute(CuDeviceAttribute.ComputeCapabilityMinor);

            result.Add(new CudaDevice(device.Handle, name, new Version(major, minor)));
        }

        return result;
    }

    internal static string GetDeviceDetails(CudaDevice device)
    {
        var sb = new StringBuilder();

        try
        {
            var dev = CuDevice.GetDevice(device.Handle);
            using var context = dev.CreateContext();
            var sessionParams = new NvEncOpenEncodeSessionExParams
            {
                Version = LibNvEnc.NV_ENC_OPEN_ENCODE_SESSION_EX_PARAMS_VER,
                ApiVersion = LibNvEnc.NVENCAPI_VERSION,
                Device = context.Handle,
                DeviceType = NvEncDeviceType.Cuda
            };

            var encoder = LibNvEnc.OpenEncoder(ref sessionParams);
            try
            {
                sb.AppendLine("  Encoding:");
                IReadOnlyList<Guid> codecGuids = encoder.GetEncodeGuids();
                foreach ((string codecName, Guid codecGuid)  in AllCodecs)
                {
                    if (codecGuids.Contains(codecGuid))
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"    - Supports {codecName} 8-bit");

                        var cap = new NvEncCapsParam { CapsToQuery = NvEncCaps.Support10bitEncode };
                        var capsVal = 0;
                        encoder.GetEncodeCaps(codecGuid, ref cap, ref capsVal);
                        if (capsVal > 0)
                        {
                            sb.AppendLine(CultureInfo.InvariantCulture, $"    - Supports {codecName} 10-bit");
                        }
                    }
                }
            }
            finally
            {
                encoder.DestroyEncoder();
            }
        }
        catch (Exception)
        {
            // do nothing
        }

        return sb.ToString();
    }
}
