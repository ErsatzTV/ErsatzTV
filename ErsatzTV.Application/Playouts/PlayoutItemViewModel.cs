using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Application.Playouts;

public record PlayoutItemViewModel(string Title, DateTimeOffset Start, DateTimeOffset Finish, string Duration, Option<FillerKind> FillerKind);
