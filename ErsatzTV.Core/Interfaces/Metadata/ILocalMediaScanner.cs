﻿using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalMediaScanner
    {
        Task<Unit> ScanLocalMediaSource(LocalMediaSource localMediaSource, string ffprobePath);
    }
}
