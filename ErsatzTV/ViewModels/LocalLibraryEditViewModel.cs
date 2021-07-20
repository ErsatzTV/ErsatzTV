using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels
{
    public class LocalLibraryEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LibraryMediaKind MediaKind { get; set; }
        public string NewPath { get; set; }
        public List<LocalLibraryPathEditViewModel> Paths { get; set; }
        public bool HasChanges { get; set; }
    }
}
