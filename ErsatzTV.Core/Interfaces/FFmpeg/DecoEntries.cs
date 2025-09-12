using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public sealed record DecoEntries(Option<Deco> TemplateDeco, Option<Deco> PlayoutDeco);
