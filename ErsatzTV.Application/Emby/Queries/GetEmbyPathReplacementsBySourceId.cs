using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Emby;

public record GetEmbyPathReplacementsBySourceId
    (int EmbyMediaSourceId) : IRequest<List<EmbyPathReplacementViewModel>>;