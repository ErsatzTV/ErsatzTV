using YamlDotNet.Serialization;

namespace ErsatzTV.Core.FFmpeg;

public class MpegTsScript
{
    [YamlIgnore]
    public string Id { get; set; }

    [YamlMember(Alias = "name", ApplyNamingConventions = false)]
    public string Name { get; set; }

    [YamlMember(Alias = "linux_script", ApplyNamingConventions = false)]
    public string LinuxScript { get; set; }

    [YamlMember(Alias = "windows_script", ApplyNamingConventions = false)]
    public string WindowsScript { get; set; }
}
