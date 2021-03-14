using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Plex
{
    public abstract class PlexLibraryScanner
    {
        protected void UpdateArtworkIfNeeded(
            Domain.Metadata existingMetadata,
            Domain.Metadata incomingMetadata,
            ArtworkKind artworkKind)
        {
            Option<Artwork> maybeIncomingArtwork = Optional(incomingMetadata.Artwork).Flatten()
                .Find(a => a.ArtworkKind == artworkKind);

            maybeIncomingArtwork.Match(
                incomingArtwork =>
                {
                    Option<Artwork> maybeExistingArtwork = Optional(existingMetadata.Artwork).Flatten()
                        .Find(a => a.ArtworkKind == artworkKind);

                    maybeExistingArtwork.Match(
                        existingArtwork =>
                        {
                            existingArtwork.Path = incomingArtwork.Path;
                            existingArtwork.DateUpdated = incomingArtwork.DateUpdated;
                        },
                        () =>
                        {
                            existingMetadata.Artwork ??= new List<Artwork>();
                            existingMetadata.Artwork.Add(incomingArtwork);
                        });
                },
                () =>
                {
                    existingMetadata.Artwork ??= new List<Artwork>();
                    existingMetadata.Artwork.RemoveAll(a => a.ArtworkKind == artworkKind);
                });
        }
    }
}
