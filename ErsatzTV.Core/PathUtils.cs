using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ErsatzTV.Core;

public static class PathUtils
{
    public static string GetPathHash(string path)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        var builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
