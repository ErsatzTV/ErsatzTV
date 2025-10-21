using System.Text;
using Bugsnag;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Application.Streaming;

public class GetLastPtsTimeHandler(
    IClient client,
    ILocalFileSystem localFileSystem,
    ITempFilePool tempFilePool,
    IConfigElementRepository configElementRepository,
    ILogger<GetLastPtsTimeHandler> logger)
    : IRequestHandler<GetLastPtsTime, Either<BaseError, PtsTime>>
{
    public async Task<Either<BaseError, PtsTime>> Handle(
        GetLastPtsTime request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request, cancellationToken);
        return await validation.Match(
            parameters => Handle(parameters, cancellationToken),
            error => Task.FromResult<Either<BaseError, PtsTime>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(
        GetLastPtsTime request,
        CancellationToken cancellationToken) =>
        await ValidateFFprobePath(cancellationToken)
            .MapT(ffprobePath => new RequestParameters(request.InitSegmentCache, request.ChannelNumber, ffprobePath));

    private async Task<Either<BaseError, PtsTime>> Handle(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        Option<FileInfo> maybeLastSegment = GetLastSegment(parameters);
        foreach (FileInfo segment in maybeLastSegment)
        {
            return await GetPts(parameters, segment, cancellationToken).IfNoneAsync(PtsTime.Zero);
        }

        return BaseError.New($"Failed to determine last pts duration for channel {parameters.ChannelNumber}");
    }

    private async Task<Option<PtsTime>> GetPts(
        RequestParameters parameters,
        FileInfo segment,
        CancellationToken cancellationToken)
    {
        string[] argumentList =
        {
            "-v", "0",
            "-show_entries",
            "packet=pts_time,duration_time",
            "-of", "compact=p=0:nk=1",
            segment.FullName
        };

        PtsTime maxTime = PtsTime.Zero;
        Action<string> replaceLine = s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    var newPts = PtsTime.From(s.Trim());
                    if (newPts.Value > maxTime.Value)
                    {
                        maxTime = newPts;
                    }
                }
                catch
                {
                    // do nothing
                }
            }
        };

        logger.LogDebug("ffprobe arguments {FFmpegArguments}", argumentList.ToList());

        CommandResult probe = await Cli.Wrap(parameters.FFprobePath)
            .WithArguments(argumentList)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(replaceLine))
            .ExecuteAsync(cancellationToken);

        if (probe.ExitCode != 0)
        {
            return Option<PtsTime>.None;
        }

        return maxTime;
    }

    private Option<FileInfo> GetLastSegment(RequestParameters parameters)
    {
        var directory = new DirectoryInfo(Path.Combine(FileSystemLayout.TranscodeFolder, parameters.ChannelNumber));
        var allFiles = directory.GetFiles("*.ts").Append(directory.GetFiles("*.m4s")).ToList();
        Option<FileInfo> maybeLastSegment = Optional(allFiles.OrderByDescending(f => f.Name).FirstOrDefault());
        foreach (var lastSegment in maybeLastSegment)
        {
            if (lastSegment.Name.Contains("m4s"))
            {
                string[] split = lastSegment.Name.Split('_');
                if (long.TryParse(split[1], out long generatedAt))
                {
                    try
                    {
                        string init = parameters.InitSegmentCache.EarliestSegmentByHash(generatedAt);
                        string fullInit = Path.Combine(directory.FullName, init);
                        string combined = tempFilePool.GetNextTempFile(TempFileCategory.Fmp4LastSegment);

                        using (var output = File.OpenWrite(combined))
                        {
                            // copy init
                            using (var readInit = File.OpenRead(fullInit))
                            {
                                readInit.CopyTo(output);
                            }

                            // copy segment
                            using (var readSegment = File.OpenRead(lastSegment.FullName))
                            {
                                readSegment.CopyTo(output);
                            }
                        }

                        // return concatenated init + segment
                        return new FileInfo(combined);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine($"Can't find init for last segment {lastSegment.FullName}");
                        foreach (var file in allFiles)
                        {
                            Console.WriteLine(file.FullName);
                        }
                        throw;
                    }
                }
            }

            return lastSegment;
        }

        return Option<FileInfo>.None;
    }

    private Task<Validation<BaseError, string>> ValidateFFprobePath(CancellationToken cancellationToken) =>
        configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath, cancellationToken)
            .FilterT(File.Exists)
            .Map(ffprobePath => ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

    private async Task SaveTroubleshootingData(string channelNumber, string output)
    {
        try
        {
            var directory = new DirectoryInfo(Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber));
            FileInfo[] allFiles = directory.GetFiles();

            string playlistFileName = Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber, "live.m3u8");
            string playlistContents = string.Empty;
            if (localFileSystem.FileExists(playlistFileName))
            {
                playlistContents = await File.ReadAllTextAsync(playlistFileName);
            }

            var data = new TroubleshootingData(allFiles, playlistContents, output);
            string serialized = data.Serialize();

            string file = tempFilePool.GetNextTempFile(TempFileCategory.BadTranscodeFolder);
            await File.WriteAllTextAsync(file, serialized);

            logger.LogWarning("Transcode folder is in bad state; troubleshooting info saved to {File}", file);
        }
        catch (Exception ex)
        {
            client.Notify(ex);
        }
    }

    private sealed record RequestParameters(
        IHlsInitSegmentCache InitSegmentCache,
        string ChannelNumber,
        string FFprobePath);

    private sealed record TroubleshootingData(IEnumerable<FileInfo> Files, string Playlist, string ProbeOutput)
    {
        public string Serialize()
        {
            var data = new InternalData(
                Files.Map(f => new FileData(f.FullName, f.Length, f.LastWriteTimeUtc)).ToList(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Playlist)),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(ProbeOutput)));

            return JsonConvert.SerializeObject(data);
        }

        private sealed record FileData(string FileName, long Bytes, DateTime LastWriteTimeUtc);

        private sealed record InternalData(List<FileData> Files, string EncodedPlaylist, string EncodedProbeOutput);
    }
}
