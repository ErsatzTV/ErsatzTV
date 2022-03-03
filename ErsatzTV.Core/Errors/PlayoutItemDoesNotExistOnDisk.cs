namespace ErsatzTV.Core.Errors;

public class PlayoutItemDoesNotExistOnDisk : BaseError
{
    public PlayoutItemDoesNotExistOnDisk(string path) : base($"Playout item does not exist on disk\n{path}")
    {
    }
}