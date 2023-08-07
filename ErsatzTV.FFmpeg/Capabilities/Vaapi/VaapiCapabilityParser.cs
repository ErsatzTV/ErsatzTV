using System.Text.RegularExpressions;

namespace ErsatzTV.FFmpeg.Capabilities.Vaapi;

public static class VaapiCapabilityParser
{
    public static List<VaapiProfileEntrypoint> Parse(string output)
    {
        var profileEntrypoints = new List<VaapiProfileEntrypoint>();

        foreach (string line in string.Join("", output).Split("\n"))
        {
            const string PROFILE_ENTRYPOINT_PATTERN = @"(VAProfile\w*).*(VAEntrypoint\w*)";
            Match match = Regex.Match(line, PROFILE_ENTRYPOINT_PATTERN);
            if (match.Success)
            {
                profileEntrypoints.Add(
                    new VaapiProfileEntrypoint(
                        match.Groups[1].Value.Trim(),
                        match.Groups[2].Value.Trim()));
            }
        }

        return profileEntrypoints;
    }

    public static List<VaapiProfileEntrypoint> ParseFull(string output)
    {
        var profileEntrypoints = new List<VaapiProfileEntrypoint>();
        var profile = new VaapiProfileEntrypoint(string.Empty, string.Empty);
        string[] allLines = string.Join("", output).Split("\n");

        for (var i = 0; i < allLines.Length; i++)
        {
            string line = allLines[i];
            const string PROFILE_ENTRYPOINT_PATTERN = @"(VAProfile\w*).*(VAEntrypoint\w*)";
            const string PROFILE_RATE_CONTROL_PATTERN = @".*VA_RC_(\w*).*";
            Match match = Regex.Match(line, PROFILE_ENTRYPOINT_PATTERN);
            if (match.Success)
            {
                profile = new VaapiProfileEntrypoint(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                profileEntrypoints.Add(profile);
            }
            else
            {
                // check for rate control
                match = Regex.Match(line, PROFILE_RATE_CONTROL_PATTERN);
                if (match.Success)
                {
                    switch (match.Groups[1].Value.Trim().ToLowerInvariant())
                    {
                        case "cqp":
                            profile.AddRateControlMode(RateControlMode.CQP);
                            break;
                        case "vbr":
                            profile.AddRateControlMode(RateControlMode.VBR);
                            break;
                        case "cbr":
                            profile.AddRateControlMode(RateControlMode.CBR);
                            break;
                    }
                }
            }
        }

        return profileEntrypoints;
    }
}
