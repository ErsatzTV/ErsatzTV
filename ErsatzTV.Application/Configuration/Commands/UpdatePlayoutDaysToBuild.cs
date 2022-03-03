﻿using ErsatzTV.Core;

namespace ErsatzTV.Application.Configuration;

public record UpdatePlayoutDaysToBuild(int DaysToBuild) : MediatR.IRequest<Either<BaseError, Unit>>;