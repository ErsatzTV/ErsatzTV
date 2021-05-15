namespace ErsatzTV.Infrastructure.Jellyfin.Models
{
    public class JellyfinUserResponse
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public JellyfinUserPolicyResponse Policy { get; set; }
    }
}
