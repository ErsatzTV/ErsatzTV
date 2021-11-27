﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.FFmpeg
{
    public interface IFFmpegProcessService
    {
        Task<Process> ForPlayoutItem(
            string ffmpegPath,
            bool saveReports,
            Channel channel,
            MediaVersion videoVersion,
            MediaVersion audioVersion,
            string videoPath,
            string audioPath,
            DateTimeOffset start,
            DateTimeOffset finish,
            DateTimeOffset now,
            Option<ChannelWatermark> globalWatermark,
            VaapiDriver vaapiDriver,
            string vaapiDevice,
            bool hlsRealtime,
            FillerKind fillerKind,
            TimeSpan inPoint,
            TimeSpan outPoint);

        Process ForError(
            string ffmpegPath,
            Channel channel,
            Option<TimeSpan> duration,
            string errorMessage,
            bool hlsRealtime);

        Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host);

        Process ConvertToPng(string ffmpegPath, string inputFile, string outputFile);

        Task<Either<BaseError, string>> GenerateSongImage(
            string ffmpegPath,
            Option<string> subtitleFile,
            Channel channel,
            Option<ChannelWatermark> globalWatermark,
            MediaVersion videoVersion,
            string videoPath);
    }
}
