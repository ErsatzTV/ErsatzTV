namespace ErsatzTV.Core.Domain;

public enum CollectionType
{
    Collection = 0,
    TelevisionShow = 1,
    TelevisionSeason = 2,
    Artist = 3,
    MultiCollection = 4,
    SmartCollection = 5,
    Playlist = 6,
    RerunFirstRun = 7,
    RerunRerun = 8,
    SearchQuery = 9,

    Movie = 10,
    Episode = 20,
    MusicVideo = 30,
    OtherVideo = 40,
    Song = 50,
    Image = 60,
    RemoteStream = 70,

    FakeCollection = 100,
    FakePlaylistItem = 101
}
