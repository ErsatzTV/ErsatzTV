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

public class GetLastPtsDurationHandler : IRequestHandler<GetLastPtsDuration, Either<BaseError, PtsAndDuration>>
{
    private readonly IClient _client;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILogger<GetLastPtsDurationHandler> _logger;
    private readonly ITempFilePool _tempFilePool;

    public GetLastPtsDurationHandler(
        IClient client,
        ILocalFileSystem localFileSystem,
        ITempFilePool tempFilePool,
        IConfigElementRepository configElementRepository,
        ILogger<GetLastPtsDurationHandler> logger)
    {
        _client = client;
        _localFileSystem = localFileSystem;
        _tempFilePool = tempFilePool;
        _configElementRepository = configElementRepository;
        _logger = logger;
    }

    public async Task<Either<BaseError, PtsAndDuration>> Handle(
        GetLastPtsDuration request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            parameters => Handle(parameters, cancellationToken),
            error => Task.FromResult<Either<BaseError, PtsAndDuration>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(GetLastPtsDuration request) =>
        await ValidateFFprobePath().MapT(ffprobePath => new RequestParameters(request.ChannelNumber, ffprobePath));

    private async Task<Either<BaseError, PtsAndDuration>> Handle(
        RequestParameters parameters,
        CancellationToken cancellationToken)
    {
        Option<FileInfo> maybeLastSegment = GetLastSegment(parameters.ChannelNumber);
        foreach (FileInfo segment in maybeLastSegment)
        {
            string[] argumentList =
            {
                "-v", "0",
                "-show_entries",
                "packet=pts,duration",
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
                return BaseError.New($"FFprobe at {parameters.FFprobePath} exited with code {probe.ExitCode}");
            }

            try
            {
                return PtsAndDuration.From(lastLine);
            }
            catch (Exception ex)
            {
                _client.Notify(ex);
                await SaveTroubleshootingData(parameters.ChannelNumber, lastLine);
            }
        }

        return BaseError.New($"Failed to determine last pts duration for channel {parameters.ChannelNumber}");
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

    private record RequestParameters(string ChannelNumber, string FFprobePath);

    private record TroubleshootingData(IEnumerable<FileInfo> Files, string Playlist, string ProbeOutput)
    {
        public string Serialize()
        {
            var data = new InternalData(
                Files.Map(f => new FileData(f.FullName, f.Length, f.LastWriteTimeUtc)).ToList(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Playlist)),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(ProbeOutput)));

            return JsonConvert.SerializeObject(data);
        }

        private record FileData(string FileName, long Bytes, DateTime LastWriteTimeUtc);

        private record InternalData(List<FileData> Files, string EncodedPlaylist, string EncodedProbeOutput);
    }
}
