using ErsatzTV.Core.Api.SmartCollections;

namespace ErsatzTV.Application.MediaCollections;

public record GetAllSmartCollectionsForApi : IRequest<List<SmartCollectionResponseModel>>;
