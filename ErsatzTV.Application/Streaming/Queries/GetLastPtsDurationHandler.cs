using System.Diagnostics;
using System.Text;
using Bugsnag;
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
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ITempFilePool _tempFilePool;
    private readonly IConfigElementRepository _configElementRepository;
    private readonly ILogger<GetLastPtsDurationHandler> _logger;

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
            var startInfo = new ProcessStartInfo
            {
                FileName = parameters.FFprobePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("0");
            startInfo.ArgumentList.Add("-show_entries");
            startInfo.ArgumentList.Add("packet=pts,duration");
            startInfo.ArgumentList.Add("-of");
            startInfo.ArgumentList.Add("compact=p=0:nk=1");
            startInfo.ArgumentList.Add("-read_intervals");
            startInfo.ArgumentList.Add("-999999");
            startInfo.ArgumentList.Add(segment.FullName);

            var probe = new Process
            {
                StartInfo = startInfo
            };

            probe.Start();
            string output = await probe.StandardOutput.ReadToEndAsync();
            await probe.WaitForExitAsync(cancellationToken);
            if (probe.ExitCode != 0)
            {
                return BaseError.New($"FFprobe at {parameters.FFprobePath} exited with code {probe.ExitCode}");
            }

            try
            {
                string[] lines = output.Split("\n");
                IEnumerable<string> nonEmptyLines = lines.Filter(s => !string.IsNullOrWhiteSpace(s)).Map(l => l.Trim());
                return PtsAndDuration.From(nonEmptyLines.Last());
            }
            catch (Exception ex)
            {
                _client.Notify(ex);
                await SaveTroubleshootingData(parameters.ChannelNumber, output);
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
        private record FileData(string FileName, long Bytes, DateTime LastWriteTimeUtc);
        private record InternalData(List<FileData> Files, string EncodedPlaylist, string EncodedProbeOutput);

        public string Serialize()
        {
            var data = new InternalData(
                Files.Map(f => new FileData(f.FullName, f.Length, f.LastWriteTimeUtc)).ToList(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Playlist)),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(ProbeOutput)));

            return JsonConvert.SerializeObject(data);
        }
    }
}
