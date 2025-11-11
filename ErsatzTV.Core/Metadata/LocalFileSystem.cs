using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public class LocalFileSystem(IClient client, ILogger<LocalFileSystem> logger) : ILocalFileSystem
{
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
            logger.LogWarning(ex, "Failed to ensure folder exists at {Folder}", folder);
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
                logger.LogWarning("Unauthorized access exception listing subdirectories of folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                client.Notify(ex);
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
                    .Where(path => !Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase));
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogWarning("Unauthorized access exception listing files in folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                client.Notify(ex);
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
                    .Where(path => !Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase));
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogWarning("Unauthorized access exception listing files in folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                client.Notify(ex);
            }
        }

        return new List<string>();
    }

    public IEnumerable<string> ListFiles(string folder, params string[] searchPatterns)
    {
        if (folder is not null && Directory.Exists(folder))
        {
            try
            {
                return searchPatterns
                    .SelectMany(searchPattern =>
                        Directory.EnumerateFiles(folder, searchPattern, SearchOption.TopDirectoryOnly)
                            .Where(path => !Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase)))
                    .Distinct();
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogWarning("Unauthorized access exception listing files in folder {Folder}", folder);
            }
            catch (Exception ex)
            {
                // do nothing
                client.Notify(ex);
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
            client.Notify(ex);
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
            logger.LogWarning(ex, "Failed to empty folder at {Folder}", folder);
        }

        return Unit.Default;
    }

    public Task<string> ReadAllText(string path) => File.ReadAllTextAsync(path);
    public Task<string[]> ReadAllLines(string path) => File.ReadAllLinesAsync(path);

    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
    public async Task<byte[]> GetHash(string path)
    {
        using var md5 = MD5.Create();
        await using var stream = File.OpenRead(path);
        return await md5.ComputeHashAsync(stream);
    }

    public string GetCustomOrDefaultFile(string folder, string file)
    {
        string path = Path.Combine(folder, file);
        return FileExists(path) ? path : Path.Combine(folder, $"_{file}");
    }
}
