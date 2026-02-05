using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Application.Playouts;

public record PlayoutItemViewModel(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset Finish,
    string Duration,
    string SchedulingContext,
    Option<FillerKind> FillerKind);
