namespace ErsatzTV.Core.Plex
{
    public record PlexAuthPin(int Id, string Code, string ClientIdentifier)
    {
        public string Url
        {
            get
            {
                var clientId = $"clientID={ClientIdentifier}";
                var code = $"code={Code}";
                var cdp = "context%5Bdevice%5D%5Bproduct%5D=ErsatzTV";
                return $"https://app.plex.tv/auth#?{clientId}&{code}&{cdp}";
            }
        }
    }
}
