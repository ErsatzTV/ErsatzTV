using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles;

public record GetAllFFmpegProfiles : IRequest<List<FFmpegProfileViewModel>>;