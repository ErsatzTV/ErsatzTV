using ErsatzTV.Application.Playouts;

namespace ErsatzTV.Application.Troubleshooting.Queries;

public record DecodePlayoutHistory(PlayoutHistoryViewModel PlayoutHistory) : IRequest<PlayoutHistoryDetailsViewModel>;
