namespace ErsatzTV.FFmpeg.Capabilities.Vaapi;

public record VaapiProfileEntrypoint(string VaapiProfile, string VaapiEntrypoint)
{
    private readonly System.Collections.Generic.HashSet<RateControlMode> _rateControlModes = new();

    public IReadOnlyCollection<RateControlMode> RateControlModes => _rateControlModes;

    public bool AddRateControlMode(RateControlMode mode) => _rateControlModes.Add(mode);
}
