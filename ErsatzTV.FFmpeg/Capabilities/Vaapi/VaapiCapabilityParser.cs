using System.Text.RegularExpressions;

namespace ErsatzTV.FFmpeg.Capabilities.Vaapi;

public static partial class VaapiCapabilityParser
{
    public static List<VaapiProfileEntrypoint> Parse(string output)
    {
        var profileEntrypoints = new List<VaapiProfileEntrypoint>();

        foreach (string line in string.Join("", output).Split("\n"))
        {
            Match match = ProfileEntrypointRegex().Match(line);
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
            Match match = ProfileEntrypointRegex().Match(line);
            if (match.Success)
            {
                profile = new VaapiProfileEntrypoint(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                profileEntrypoints.Add(profile);
            }
            else
            {
                // check for rate control
                match = ProfileRateControlRegex().Match(line);
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

    public static string ParseGeneration(string output)
    {
        string generation = string.Empty;
        Match match = MiscGenerationRegex().Match(output);
        if (match.Success)
        {
            generation = match.Groups[1].Value.Trim().ToLowerInvariant();
            if (generation is "radeonsi")
            {
                match = RadeonSiGenerationRegex().Match(output);
                if (match.Success)
                {
                    generation = match.Groups[1].Value.Trim().ToLowerInvariant();
                }
            }
        }

        return generation;
    }

    [GeneratedRegex(@"(VAProfile\w*).*(VAEntrypoint\w*)")]
    private static partial Regex ProfileEntrypointRegex();

    [GeneratedRegex(@".*VA_RC_(\w*).*")]
    private static partial Regex ProfileRateControlRegex();

    [GeneratedRegex(@"Driver version:.*\(radeonsi, (\w+)")]
    private static partial Regex RadeonSiGenerationRegex();

    [GeneratedRegex(@"Driver version:.*\((\w+),")]
    private static partial Regex MiscGenerationRegex();
}
