using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Libraries;

public record GetLocalLibraryById(int LibraryId) : IRequest<Option<LocalLibraryViewModel>>;