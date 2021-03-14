using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Plex
{
    public abstract class PlexLibraryScanner
    {
        private readonly IMetadataRepository _metadataRepository;

        protected PlexLibraryScanner(IMetadataRepository metadataRepository) =>
            _metadataRepository = metadataRepository;

        protected async Task<Unit> UpdateArtworkIfNeeded(
            Domain.Metadata existingMetadata,
            Domain.Metadata incomingMetadata,
            ArtworkKind artworkKind)
        {
            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                Option<Artwork> maybeIncomingArtwork = Optional(incomingMetadata.Artwork).Flatten()
                    .Find(a => a.ArtworkKind == artworkKind);

                await maybeIncomingArtwork.Match(
                    async incomingArtwork =>
                    {
                        Option<Artwork> maybeExistingArtwork = Optional(existingMetadata.Artwork).Flatten()
                            .Find(a => a.ArtworkKind == artworkKind);

                        await maybeExistingArtwork.Match(
                            async existingArtwork =>
                            {
                                existingArtwork.Path = incomingArtwork.Path;
                                existingArtwork.DateUpdated = incomingArtwork.DateUpdated;
                                await _metadataRepository.UpdateArtworkPath(existingArtwork);
                            },
                            async () =>
                            {
                                existingMetadata.Artwork ??= new List<Artwork>();
                                existingMetadata.Artwork.Add(incomingArtwork);
                                await _metadataRepository.AddArtwork(existingMetadata, incomingArtwork);
                            });
                    },
                    async () =>
                    {
                        existingMetadata.Artwork ??= new List<Artwork>();
                        existingMetadata.Artwork.RemoveAll(a => a.ArtworkKind == artworkKind);
                        await _metadataRepository.RemoveArtwork(existingMetadata, artworkKind);
                    });
            }

            return Unit.Default;
        }
    }
}
