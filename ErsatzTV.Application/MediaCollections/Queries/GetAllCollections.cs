﻿using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetAllCollections : IRequest<List<MediaCollectionViewModel>>;
}
