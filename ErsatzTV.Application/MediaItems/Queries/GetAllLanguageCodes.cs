using System.Collections.Generic;
using System.Globalization;
using MediatR;

namespace ErsatzTV.Application.MediaItems;

public record GetAllLanguageCodes : IRequest<List<CultureInfo>>;