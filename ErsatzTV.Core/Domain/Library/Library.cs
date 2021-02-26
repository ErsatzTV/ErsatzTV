using System;
using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public abstract class Library
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LibraryMediaKind MediaKind { get; set; }
        public DateTime? LastScan { get; set; }

        public int MediaSourceId { get; set; }
        public MediaSource MediaSource { get; set; }

        public List<LibraryPath> Paths { get; set; }
    }
}
