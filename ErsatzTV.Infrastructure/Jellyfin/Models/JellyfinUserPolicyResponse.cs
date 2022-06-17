namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinUserPolicyResponse
{
    public bool IsAdministrator { get; set; }
    public bool IsDisabled { get; set; }
    public bool EnableAllFolders { get; set; }
}
