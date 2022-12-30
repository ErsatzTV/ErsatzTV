﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;

namespace ErsatzTV.Scanner.Tests.Core.Fakes;

public class FakeLocalFileSystem : ILocalFileSystem
{
    public static readonly byte[] TestBytes = { 1, 2, 3, 4, 5 };

    private readonly List<FakeFileEntry> _files;
    private readonly List<FakeFolderEntry> _folders;

    public FakeLocalFileSystem(List<FakeFileEntry> files) : this(files, new List<FakeFolderEntry>())
    {
    }

    public FakeLocalFileSystem(List<FakeFileEntry> files, List<FakeFolderEntry> folders)
    {
        _files = files;

        var allFolders = new List<string>(folders.Map(f => f.Path));
        foreach (FakeFileEntry file in _files)
        {
            List<DirectoryInfo> moreFolders =
                Split(new DirectoryInfo(Path.GetDirectoryName(file.Path) ?? string.Empty));
            allFolders.AddRange(moreFolders.Map(i => i.FullName));
        }

        _folders = allFolders.Distinct().Map(f => new FakeFolderEntry(f)).ToList();
    }

    public Unit EnsureFolderExists(string folder) => Unit.Default;

    public DateTime GetLastWriteTime(string path) =>
        Optional(_files.SingleOrDefault(f => f.Path == path))
            .Map(f => f.LastWriteTime)
            .IfNone(SystemTime.MinValueUtc);

    public bool IsLibraryPathAccessible(LibraryPath libraryPath) =>
        _folders.Any(f => f.Path == libraryPath.Path);

    public IEnumerable<string> ListSubdirectories(string folder) =>
        _folders.Map(f => f.Path).Filter(f => f.StartsWith(folder) && Directory.GetParent(f)?.FullName == folder);

    public IEnumerable<string> ListFiles(string folder) =>
        _files.Map(f => f.Path).Filter(f => Path.GetDirectoryName(f) == folder);

    // TODO: this isn't accurate, need to use search pattern
    public IEnumerable<string> ListFiles(string folder, string searchPattern) =>
        _files.Map(f => f.Path).Filter(f => Path.GetDirectoryName(f) == folder);

    public bool FileExists(string path) => _files.Any(f => f.Path == path);
    public bool FolderExists(string folder) => false;

    public Task<Either<BaseError, Unit>> CopyFile(string source, string destination) =>
        Task.FromResult(Right<BaseError, Unit>(Unit.Default));

    public Unit EmptyFolder(string folder) => Unit.Default;

    public Task<byte[]> ReadAllBytes(string path) => TestBytes.AsTask();

    private static List<DirectoryInfo> Split(DirectoryInfo path)
    {
        var result = new List<DirectoryInfo>();
        if (path == null || string.IsNullOrWhiteSpace(path.FullName))
        {
            return result;
        }

        if (path.Parent != null)
        {
            result.AddRange(Split(path.Parent));
        }

        result.Add(path);

        return result;
    }
}
