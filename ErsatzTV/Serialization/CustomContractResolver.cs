using Newtonsoft.Json.Serialization;

namespace ErsatzTV.Serialization
{
    public class CustomContractResolver : DefaultContractResolver
    {
        public CustomContractResolver() => NamingStrategy = new CustomNamingStrategy();
    }
}
