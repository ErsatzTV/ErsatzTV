﻿namespace ErsatzTV.Application.Television;

public record GetTelevisionSeasonById(int SeasonId) : IRequest<Option<TelevisionSeasonViewModel>>;