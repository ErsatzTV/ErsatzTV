namespace ErsatzTV.Core.Scripting;

public record EpisodeAudioStreamSelectorData(
    string ShowTitle,
    string[] ShowGuids,
    int SeasonNumber,
    int EpisodeNumber,
    string[] EpisodeGuids);
