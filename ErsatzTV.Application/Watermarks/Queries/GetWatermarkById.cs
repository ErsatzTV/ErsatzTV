using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Watermarks.Queries
{
    public record GetWatermarkById(int Id) : IRequest<Option<WatermarkViewModel>>;
}
