using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Resolutions;

public class CreateCustomResolutionHandler : IRequestHandler<CreateCustomResolution, Option<BaseError>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public CreateCustomResolutionHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Option<BaseError>> Handle(CreateCustomResolution request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Validation<BaseError, Resolution> validation = await Validate(dbContext, request);
        return await validation.Match(
            r => PersistResolution(dbContext, r, cancellationToken),
            error => Task.FromResult<Option<BaseError>>(error.Join()));
    }

    private static async Task<Option<BaseError>> PersistResolution(
        TvContext dbContext,
        Resolution resolution,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Resolutions.AddAsync(resolution, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.Message);
        }
    }

    private static Task<Validation<BaseError, Resolution>> Validate(
        TvContext dbContext,
        CreateCustomResolution request) =>
        ResolutionMustBeUnique(dbContext, request)
            .MapT(
                _ => new Resolution
                {
                    Name = $"{request.Width}x{request.Height}",
                    Width = request.Width,
                    Height = request.Height,
                    IsCustom = true
                });

    private static async Task<Validation<BaseError, Unit>> ResolutionMustBeUnique(
        TvContext dbContext,
        CreateCustomResolution request)
    {
        Option<Resolution> maybeExisting = await dbContext.Resolutions
            .FirstOrDefaultAsync(r => r.Height == request.Height && r.Width == request.Width)
            .Map(Optional);

        if (maybeExisting.IsSome)
        {
            return BaseError.New("Resolution width and height must be unique");
        }

        if (request.Height <= 0 || request.Width <= 0)
        {
            return BaseError.New("Resolution width or height is invalid");
        }

        return Unit.Default;
    }
}
