﻿using System;
using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Queries
{
    public record GetSimpleMediaCollectionWithItemsById
        (int Id) : IRequest<Option<Tuple<MediaCollectionViewModel, List<MediaItemSearchResultViewModel>>>>;
}
