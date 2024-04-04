using ErsatzTV.Core;

namespace ErsatzTV.Application.Scheduling;

public record DeleteDeco(int DecoId) : IRequest<Option<BaseError>>;
