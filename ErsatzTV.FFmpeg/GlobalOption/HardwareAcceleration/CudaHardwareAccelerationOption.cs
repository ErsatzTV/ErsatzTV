namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class CudaHardwareAccelerationOption(bool isVulkanHdr) : GlobalOption
{
    public override string[] GlobalOptions
    {
        get
        {
            if (isVulkanHdr)
            {
                return ["-init_hw_device", "cuda=nv", "-init_hw_device", "vulkan=vk@nv", "-hwaccel", "vulkan"];
            }

            return ["-init_hw_device", "cuda", "-hwaccel", "cuda"];
        }
    }
}
