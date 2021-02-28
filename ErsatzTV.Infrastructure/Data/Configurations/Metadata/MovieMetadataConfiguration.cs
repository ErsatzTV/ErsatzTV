﻿using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MovieMetadataConfiguration : IEntityTypeConfiguration<MovieMetadata>
    {
        public void Configure(EntityTypeBuilder<MovieMetadata> builder)
        {
            builder.ToTable("MovieMetadata");

            builder.HasMany(mm => mm.Artwork)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
