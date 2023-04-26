using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public class LocalFileSystem : ILocalFileSystem
{
    private readonly IClient _client;
    private readonly ILogger<LocalFileSystem> _logger;

    public LocalFileSystem(IClient client, ILogger<LocalFileSystem> logger)
    {
        _client = client;
        _logger = logger;
    }

    public Unit EnsureFolderExists(string folder)
    {
        try
        {
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure folder exists at {Folder}", folder);
        }

        return Unit.Default;
    }

    public DateTime GetLastWriteTime(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            return SystemTime.MinValueUtc;
        }
    }

    public bool IsLibraryPathAccessible(LibraryPath libraryPath) =>
        Directory.Exists(libraryPath.Path);

    public IEnumerable<string> ListSubdirectories(string folder)
    {
        if (Directory.Exists(folder))
        {
            try
            {
                return Directory.EnumerateDirectories(folder);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access exception listing subdirectories of folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                _client.Notify(ex);
            }
        }

        return new List<string>();
    }

    public IEnumerable<string> ListFiles(string folder)
    {
        if (Directory.Exists(folder))
        {
            try
            {
                return Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                    .Where(path => !path.StartsWith("._"));
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access exception listing files in folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                _client.Notify(ex);
            }
        }

        return new List<string>();
    }

    public IEnumerable<string> ListFiles(string folder, string searchPattern)
    {
        if (folder is not null && Directory.Exists(folder))
        {
            try
            {
                return Directory.EnumerateFiles(folder, searchPattern, SearchOption.TopDirectoryOnly)
                    .Where(path => !path.StartsWith("._"));
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access exception listing files in folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                _client.Notify(ex);
            }
        }

        return new List<string>();
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool FolderExists(string folder) => Directory.Exists(folder);

    public async Task<Either<BaseError, Unit>> CopyFile(string source, string destination)
    {
        try
        {
            string directory = Path.GetDirectoryName(destination) ?? string.Empty;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using FileStream sourceStream = File.OpenRead(source);
            await using FileStream destinationStream = File.Create(destination);
            await sourceStream.CopyToAsync(destinationStream);

            return Unit.Default;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    public Unit EmptyFolder(string folder)
    {
        try
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                File.Delete(file);
            }

            foreach (string directory in Directory.GetDirectories(folder))
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to empty folder at {Folder}", folder);
        }

        return Unit.Default;
    }
}
