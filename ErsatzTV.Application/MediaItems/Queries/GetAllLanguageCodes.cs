using System.Globalization;

namespace ErsatzTV.Application.MediaItems;

public record GetAllLanguageCodes : IRequest<List<CultureInfo>>;
