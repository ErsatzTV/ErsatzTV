using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record DeleteFFmpegProfile(int FFmpegProfileId) : IRequest<Either<BaseError, Task>>;
}
