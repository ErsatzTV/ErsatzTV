using System;
using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class LibraryPath
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public DateTime? LastScan { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }

        public List<MediaItem> MediaItems { get; set; }
        public List<LibraryFolder> LibraryFolders { get; set; }
    }
}
