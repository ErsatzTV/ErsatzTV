using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Queries
{
    public record GetFFmpegProfileById(int Id) : IRequest<Option<FFmpegProfileViewModel>>;
}
