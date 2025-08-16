using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities.VideoToolbox;

internal static partial class VideoToolboxUtil
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string VideoToolbox = "/System/Library/Frameworks/VideoToolbox.framework/VideoToolbox";
    private const string LibSystem = "/usr/lib/libSystem.dylib";

    [LibraryImport(CoreFoundation)]
    private static partial long CFArrayGetCount(IntPtr array);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFArrayGetValueAtIndex(IntPtr array, int index);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFDictionaryGetValue(IntPtr dict, IntPtr key);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFStringGetLength(IntPtr theString);

    [LibraryImport(CoreFoundation)]
    private static partial IntPtr CFStringGetCStringPtr(IntPtr theString, uint encoding);

    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static partial bool CFStringGetCString(IntPtr theString, byte[] buffer, long bufferSize, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr cf);

    [LibraryImport(VideoToolbox)]
    private static partial int VTCopyVideoEncoderList(IntPtr options, out IntPtr listOfEncoders);

    [LibraryImport(VideoToolbox)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static partial bool VTIsHardwareDecodeSupported(uint codecType);

    [LibraryImport(LibSystem, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr dlopen(string path, int mode);

    [LibraryImport(LibSystem, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr dlsym(IntPtr handle, string symbol);

    [LibraryImport(LibSystem)]
    private static partial int dlclose(IntPtr handle);

    private static IntPtr GetCFString(string frameworkPath, string symbolName)
    {
        IntPtr frameworkHandle = dlopen(frameworkPath, 0); // RTLD_NOW
        if (frameworkHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        try
        {
            IntPtr symbol = dlsym(frameworkHandle, symbolName);
            return Marshal.ReadIntPtr(symbol);
        }
        finally
        {
            _ = dlclose(frameworkHandle);
        }
    }

    private static string? CFStringToString(IntPtr cfString)
    {
        if (cfString == IntPtr.Zero)
        {
            return null;
        }

        const uint kCFStringEncodingUTF8 = 0x08000100;

        IntPtr cStringPtr = CFStringGetCStringPtr(cfString, kCFStringEncodingUTF8);
        if (cStringPtr != IntPtr.Zero)
        {
            return Marshal.PtrToStringAnsi(cStringPtr);
        }

        long length = CFStringGetLength(cfString);
        if (length == 0)
        {
            return string.Empty;
        }

        long maxSize = length * 4 + 1;
        var buffer = new byte[maxSize];
        if (CFStringGetCString(cfString, buffer, maxSize, kCFStringEncodingUTF8))
        {
            int terminator = Array.IndexOf(buffer, (byte)0);
            int actualLength = terminator >= 0 ? terminator : buffer.Length;
            return Encoding.UTF8.GetString(buffer, 0, actualLength);
        }

        return null;
    }

    private static uint FourCCToUInt32(string fourCC)
    {
        if (fourCC.Length != 4)
        {
            throw new ArgumentException("FourCC must be 4 characters long.", nameof(fourCC));
        }

        return ((uint)fourCC[0] << 24) | ((uint)fourCC[1] << 16) | ((uint)fourCC[2] << 8) | fourCC[3];
    }

    internal static List<string> GetAvailableEncoders(ILogger logger)
    {
        var encoderNames = new List<string>();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return encoderNames;
        }

        IntPtr kVTVideoEncoderList_EncoderName = GetCFString(VideoToolbox, "kVTVideoEncoderList_EncoderName");
        if (kVTVideoEncoderList_EncoderName == IntPtr.Zero)
        {
            logger.LogWarning("Failed to load kVTVideoEncoderList_EncoderName symbol.");
            return encoderNames;
        }

        IntPtr encoderList = IntPtr.Zero;
        try
        {
            int status = VTCopyVideoEncoderList(IntPtr.Zero, out encoderList);

            if (status != 0 || encoderList == IntPtr.Zero)
            {
                logger.LogWarning("VTCopyVideoEncoderList failed with status: {Status}", status);
                return encoderNames;
            }

            var count = (int)CFArrayGetCount(encoderList);
            for (var i = 0; i < count; i++)
            {
                IntPtr encoderDict = CFArrayGetValueAtIndex(encoderList, i);
                if (encoderDict == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr encoderNameCfString = CFDictionaryGetValue(encoderDict, kVTVideoEncoderList_EncoderName);
                string? encoderName = CFStringToString(encoderNameCfString);

                if (!string.IsNullOrEmpty(encoderName))
                {
                    encoderNames.Add(encoderName);
                }
            }
        }
        finally
        {
            if (encoderList != IntPtr.Zero)
            {
                CFRelease(encoderList);
            }
        }

        return encoderNames;
    }

    internal static bool IsHardwareDecoderSupported(string codecFourCC, ILogger logger)
    {
        try
        {
            uint codecType = FourCCToUInt32(codecFourCC);
            return VTIsHardwareDecodeSupported(codecType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error checking decoder support for {CodecFourCC}", codecFourCC);
            return false;
        }
    }
}
