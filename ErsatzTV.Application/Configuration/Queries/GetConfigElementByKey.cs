using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Configuration;

public record GetConfigElementByKey(ConfigElementKey Key) : IRequest<Option<ConfigElementViewModel>>;