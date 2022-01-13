using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.Maintenance.Commands;

public record DeleteItemsFromDatabase(List<int> MediaItemIds) : IRequest<Either<BaseError, Unit>>;
