using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Troubleshooting.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Application.Troubleshooting;

public class ArchiveTroubleshootingResultsHandler(IMediator mediator, ILocalFileSystem localFileSystem)
    : IRequestHandler<ArchiveTroubleshootingResults, Option<string>>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<Option<string>> Handle(ArchiveTroubleshootingResults request, CancellationToken cancellationToken)
    {
        string tempFile = Path.GetTempFileName();
        using ZipArchive zipArchive = ZipFile.Open(tempFile, ZipArchiveMode.Update);

        string transcodeFolder = Path.Combine(FileSystemLayout.TranscodeFolder, ".troubleshooting");

        bool hasReport = false;
        foreach (string file in localFileSystem.ListFiles(transcodeFolder))
        {
            // add to archive
            if (Path.GetFileName(file).StartsWith("ffmpeg-", StringComparison.InvariantCultureIgnoreCase))
            {
                hasReport = true;
                zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }

        Either<BaseError, MediaItemInfo> maybeMediaItemInfo = await mediator.Send(new GetMediaItemInfo(request.MediaItemId), cancellationToken);
        foreach (MediaItemInfo info in maybeMediaItemInfo.RightToSeq())
        {
            string infoJson = JsonSerializer.Serialize(info, Options);
            string tempMediaInfoFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempMediaInfoFile, infoJson, cancellationToken);
            zipArchive.CreateEntryFromFile(tempMediaInfoFile, "media_info.json");
        }

        TroubleshootingInfo troubleshootingInfo = await mediator.Send(new GetTroubleshootingInfo(), cancellationToken);

        string troubleshootingInfoJson = JsonSerializer.Serialize(
            new
            {
                troubleshootingInfo.Version,
                Environment = troubleshootingInfo.Environment.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value),
                troubleshootingInfo.Health,
                troubleshootingInfo.FFmpegSettings,
                troubleshootingInfo.Channels,
                troubleshootingInfo.FFmpegProfiles
            },
            Options);

        string tempTroubleshootingInfoFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempTroubleshootingInfoFile, troubleshootingInfoJson, cancellationToken);
        zipArchive.CreateEntryFromFile(tempTroubleshootingInfoFile, "troubleshooting_info.json");

        return hasReport ? tempFile : Option<string>.None;
    }
}
