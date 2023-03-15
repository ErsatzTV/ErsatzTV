using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ErsatzTV;

public static class JwtHelper
{
    public static void Init(IConfiguration configuration)
    {
        IsEnabled = !string.IsNullOrWhiteSpace(configuration["JWT:IssuerSigningKey"]);
        if (IsEnabled)
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["JWT:IssuerSigningKey"]));
        }
    }

    public static bool ValidateIssuer { get; private set; }
    public static bool ValidateAudience { get; private set; }
    public static bool ValidateIssuerSigningKey { get; private set; }
    public static bool ValidateLifetime { get; private set; }
    public static SymmetricSecurityKey IssuerSigningKey { get; private set; }
    public static bool IsEnabled { get; private set; }
}
