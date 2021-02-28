﻿using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations
{
    public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
    {
        public void Configure(EntityTypeBuilder<MediaItem> builder) => builder.ToTable("MediaItem");
        // builder.OwnsOne(c => c.Statistics).WithOwner();
        //
        // builder.HasIndex(i => i.Path)
        //     .IsUnique();
    }
}
