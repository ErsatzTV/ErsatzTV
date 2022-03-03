﻿using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Television;

public record GetTelevisionShowById(int Id) : IRequest<Option<TelevisionShowViewModel>>;