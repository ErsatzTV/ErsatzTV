using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ErsatzTV;

public static class JwtHelper
{
    public static SymmetricSecurityKey IssuerSigningKey { get; private set; }
    public static bool IsEnabled { get; private set; }

    public static void Init(IConfiguration configuration)
    {
        string issuerSigningKey = configuration["JWT:IssuerSigningKey"];
        IsEnabled = !string.IsNullOrWhiteSpace(issuerSigningKey);
        if (IsEnabled)
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(issuerSigningKey!));
        }
    }
}
