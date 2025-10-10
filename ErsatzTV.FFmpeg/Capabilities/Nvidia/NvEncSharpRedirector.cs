using System.Reflection;
using System.Runtime.InteropServices;

namespace ErsatzTV.FFmpeg.Capabilities.Nvidia;

public static class NvEncSharpRedirector
{
    static NvEncSharpRedirector()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(Lennox.NvEncSharp.LibCuda).Assembly, Resolver);
        }
        catch (DllNotFoundException)
        {
            // do nothing
        }
    }

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("nvEncodeAPI64.dll", StringComparison.OrdinalIgnoreCase))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libnvidia-encode.so.1", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeLibrary.Load("nvEncodeAPI64.dll", assembly, searchPath);
        }

        if (libraryName.Equals("nvEncodeAPI.dll", StringComparison.OrdinalIgnoreCase))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libnvidia-encode.so.1", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeLibrary.Load("nvEncodeAPI.dll", assembly, searchPath);
        }

        if (libraryName.Equals("nvcuda.dll", StringComparison.OrdinalIgnoreCase))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libcuda.so.1", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeLibrary.Load("nvcuda.dll", assembly, searchPath);
        }

        if (libraryName.Equals("nvcuvid.dll", StringComparison.OrdinalIgnoreCase))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return NativeLibrary.Load("libnvcuvid.so.1", assembly, searchPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeLibrary.Load("nvcuvid.dll", assembly, searchPath);
        }

        return IntPtr.Zero;
    }

    public static void Init() { }
}
