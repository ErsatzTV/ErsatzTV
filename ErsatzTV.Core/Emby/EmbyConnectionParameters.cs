using ErsatzTV.Core.Domain.MediaServer;

namespace ErsatzTV.Core.Emby;

public record EmbyConnectionParameters(string Address, string ApiKey) : MediaServerConnectionParameters;
