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

public class GetLastPtsTimeHandler : IRequestHandler<GetLastPtsTime, Either<BaseError, PtsTime>>
{
    private readonly IClient _client;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<GetLastPtsTimeHandler> _logger;
    private readonly ITempFilePool _tempFilePool;

    public GetLastPtsTimeHandler(
        IClient client,
        ILocalFileSystem localFileSystem,
        ITempFilePool tempFilePool,
        IConfigElementRepository configElementRepository,
        ILogger<GetLastPtsTimeHandler> logger)
    {
        _client = client;
        _localFileSystem = localFileSystem;
        _tempFilePool = tempFilePool;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, PtsTime>> Handle(
        GetLastPtsTime request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            parameters => Handle(parameters, cancellationToken),
            error => Task.FromResult<Either<BaseError, PtsTime>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(GetLastPtsTime request) =>
        await ValidateFFprobePath().MapT(ffprobePath => new RequestParameters(request.ChannelNumber, ffprobePath));

    private async Task<Either<BaseError, PtsTime>> Handle(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        Option<FileInfo> maybeLastSegment = GetLastSegment(parameters.ChannelNumber);
        foreach (FileInfo segment in maybeLastSegment)
        {
            PtsTime videoPts = await GetPts(parameters, segment, "v", cancellationToken).IfNoneAsync(PtsTime.Zero);
            PtsTime audioPts = await GetPts(parameters, segment, "a", cancellationToken).IfNoneAsync(PtsTime.Zero);
            return videoPts.Value > audioPts.Value ? videoPts : audioPts;
        }

        return BaseError.New($"Failed to determine last pts duration for channel {parameters.ChannelNumber}");
    }

    private async Task<Option<PtsTime>> GetPts(RequestParameters parameters, FileInfo segment, string audioVideo, CancellationToken cancellationToken)
    {
        string[] argumentList =
        {
            "-v", "0",
            "-select_streams", $"{audioVideo}:0",
            "-show_entries",
            "packet=pts_time,duration_time",
            "-of", "compact=p=0:nk=1",
            // "-read_intervals", "999999", // read_intervals causes inconsistent behavior on windows
            segment.FullName
        };

        string lastLine = string.Empty;
        Action<string> replaceLine = s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                lastLine = s.Trim();
            }
        };

        CommandResult probe = await Cli.Wrap(parameters.FFprobePath)
            .WithArguments(argumentList)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(replaceLine))
            .ExecuteAsync(cancellationToken);

        if (probe.ExitCode != 0)
        {
            return Option<PtsTime>.None;
        }

        try
        {
            return PtsTime.From(lastLine);
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            await SaveTroubleshootingData(parameters.ChannelNumber, lastLine);
        }

        return Option<PtsTime>.None;
    }

    private static Option<FileInfo> GetLastSegment(string channelNumber)
    {
        var directory = new DirectoryInfo(Path.Combine(FileSystemLayout.TranscodeFolder, channelNumber));
        return Optional(directory.GetFiles("*.ts").OrderByDescending(f => f.Name).FirstOrDefault());
    }

    private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
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
            if (_localFileSystem.FileExists(playlistFileName))
            {
                playlistContents = await File.ReadAllTextAsync(playlistFileName);
            }

            var data = new TroubleshootingData(allFiles, playlistContents, output);
            string serialized = data.Serialize();

            string file = _tempFilePool.GetNextTempFile(TempFileCategory.BadTranscodeFolder);
            await File.WriteAllTextAsync(file, serialized);

            _logger.LogWarning("Transcode folder is in bad state; troubleshooting info saved to {File}", file);
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
        }
    }

    private sealed record RequestParameters(string ChannelNumber, string FFprobePath);

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
