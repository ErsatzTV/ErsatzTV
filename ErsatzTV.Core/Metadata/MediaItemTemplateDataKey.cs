namespace ErsatzTV.Core.Metadata;

public static class MediaItemTemplateDataKey
{
    public static readonly string Resolution = "MediaItem_Resolution";
    public static readonly string Duration = "MediaItem_Duration";
    public static readonly string StreamSeek = "MediaItem_StreamSeek";
    public static readonly string Start = "MediaItem_Start";
    public static readonly string Stop = "MediaItem_Stop";

    // common
    public static readonly string Path = "MediaItem_Path";
    public static readonly string Title = "MediaItem_Title";
    public static readonly string Plot = "MediaItem_Plot";
    public static readonly string ReleaseDate = "MediaItem_ReleaseDate";
    public static readonly string Studios = "MediaItem_Studios";
    public static readonly string Directors = "MediaItem_Directors";
    public static readonly string Genres = "MediaItem_Genres";

    // movie
    public static readonly string ContentRating = "MediaItem_ContentRating";

    // episode/show
    public static readonly string ShowTitle = "MediaItem_ShowTitle";
    public static readonly string ShowYear = "MediaItem_ShowYear";
    public static readonly string ShowContentRating = "MediaItem_ShowContentRating";
    public static readonly string ShowGenres = "MediaItem_ShowGenres";

    // music video
    public static readonly string Track = "MediaItem_Track";
    public static readonly string Album = "MediaItem_Album";
    public static readonly string Artist = "MediaItem_Artist";
    public static readonly string Artists = "MediaItem_Artists";
}
