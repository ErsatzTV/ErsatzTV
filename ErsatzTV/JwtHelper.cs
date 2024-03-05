using System.IdentityModel.Tokens.Jwt;
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

    public static string GenerateToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(IssuerSigningKey, SecurityAlgorithms.HmacSha256Signature)
        };
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
