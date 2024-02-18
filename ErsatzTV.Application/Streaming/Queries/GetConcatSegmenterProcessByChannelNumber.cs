﻿namespace ErsatzTV.Application.Streaming;

public record GetConcatSegmenterProcessByChannelNumber : FFmpegProcessRequest
{
    public GetConcatSegmenterProcessByChannelNumber(string scheme, string host, string channelNumber) : base(
        channelNumber,
        "ts-legacy",
        DateTimeOffset.Now,
        false,
        true,
        0)
    {
        Scheme = scheme;
        Host = host;
    }

    public string Scheme { get; }
    public string Host { get; }
}
