using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Application.Playouts;

public class TimeShiftOnDemandPlayoutHandler(IPlayoutTimeShifter playoutTimeShifter)
    : IRequestHandler<TimeShiftOnDemandPlayout, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(TimeShiftOnDemandPlayout request, CancellationToken cancellationToken)
    {
        try
        {
            await playoutTimeShifter.TimeShift(request.PlayoutId, request.Now, request.Force, cancellationToken);
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }

        return Option<BaseError>.None;
    }
}
