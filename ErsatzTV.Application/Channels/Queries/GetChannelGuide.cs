﻿using ErsatzTV.Core.Iptv;

namespace ErsatzTV.Application.Channels;

public record GetChannelGuide(string Scheme, string Host, string BaseUrl) : IRequest<ChannelGuide>;
