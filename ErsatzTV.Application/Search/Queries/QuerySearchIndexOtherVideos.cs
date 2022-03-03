﻿using ErsatzTV.Application.MediaCards;
using MediatR;

namespace ErsatzTV.Application.Search;

public record QuerySearchIndexOtherVideos
    (string Query, int PageNumber, int PageSize) : IRequest<OtherVideoCardResultsViewModel>;