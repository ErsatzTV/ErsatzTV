using System;
using Newtonsoft.Json.Serialization;

namespace ErsatzTV.Serialization
{
    public class CustomNamingStrategy : CamelCaseNamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            if (name.Equals("FFmpegProfileId", StringComparison.OrdinalIgnoreCase))
            {
                return "ffmpegProfileId";
            }

            return base.ResolvePropertyName(name);
        }
    }
}
