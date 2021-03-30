﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalFileSystem : ILocalFileSystem
    {
        public Unit EnsureFolderExists(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Unit.Default;
        }

        public DateTime GetLastWriteTime(string path) =>
            Try(File.GetLastWriteTimeUtc(path)).IfFail(() => DateTime.MinValue);

        public bool IsLibraryPathAccessible(LibraryPath libraryPath) =>
            Directory.Exists(libraryPath.Path);

        public IEnumerable<string> ListSubdirectories(string folder) =>
            Try(Directory.EnumerateDirectories(folder)).IfFail(new List<string>());

        public IEnumerable<string> ListFiles(string folder) =>
            Try(Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)).IfFail(new List<string>());

        public bool FileExists(string path) => File.Exists(path);
        public Task<byte[]> ReadAllBytes(string path) => File.ReadAllBytesAsync(path);

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
                return BaseError.New(ex.ToString());
            }
        }
    }
}
