namespace ErsatzTV;

public static class OidcHelper
{
    public static void Init(IConfiguration configuration)
    {
        Authority = configuration["OIDC:Authority"];
        ClientId = configuration["OIDC:ClientId"];
        ClientSecret = configuration["OIDC:ClientSecret"];

        IsEnabled = !string.IsNullOrWhiteSpace(Authority) &&
                   !string.IsNullOrWhiteSpace(ClientId) &&
                   !string.IsNullOrWhiteSpace(ClientSecret);
    }

    public static string Authority { get; private set; }
    public static string ClientId { get; private set; }
    public static string ClientSecret { get; private set; }
    public static bool IsEnabled { get; private set; }
}
