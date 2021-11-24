﻿using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ISongFolderScanner
    {
        Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            string ffmpegPath,
            decimal progressMin,
            decimal progressMax);
    }
}
