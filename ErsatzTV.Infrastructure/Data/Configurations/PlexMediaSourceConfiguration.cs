﻿using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class PlexMediaSourceConfiguration : IEntityTypeConfiguration<PlexMediaSource>
    {
        public void Configure(EntityTypeBuilder<PlexMediaSource> builder) => builder.ToTable("PlexMediaSources");
    }
}
