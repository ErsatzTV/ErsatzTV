using LanguageExt;

namespace ErsatzTV.Application.Search;

public record RebuildSearchIndex : MediatR.IRequest<Unit>, IBackgroundServiceRequest;