﻿using System.Diagnostics.CodeAnalysis;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Hdhr;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class LineupItem
{
    private readonly Channel _channel;
    private readonly string _host;
    private readonly string _scheme;

    public LineupItem(string scheme, string host, Channel channel)
    {
        _scheme = scheme;
        _host = host;
        _channel = channel;
    }

    public string GuideNumber => _channel.Number;
    public string GuideName => _channel.Name;

    public string URL => _channel.StreamingMode switch
    {
        StreamingMode.TransportStream => $"{_scheme}://{_host}/iptv/hdhr/channel/{_channel.Number}.ts?mode=ts-legacy",
        _ => $"{_scheme}://{_host}/iptv/hdhr/channel/{_channel.Number}.ts"
    };
}
