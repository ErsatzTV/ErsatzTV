using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Watermarks;

public record GetWatermarkById(int Id) : IRequest<Option<WatermarkViewModel>>;