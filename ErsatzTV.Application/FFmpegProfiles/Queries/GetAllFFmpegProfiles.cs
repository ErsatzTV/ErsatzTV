using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public record GetAllFFmpegProfiles : IRequest<List<FFmpegProfileViewModel>>;
}
