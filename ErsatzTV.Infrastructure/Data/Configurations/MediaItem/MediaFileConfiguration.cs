﻿using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFile");

        builder.HasIndex(f => f.Path)
            .IsUnique();

        builder.HasOne(f => f.LibraryFolder)
            .WithMany(f => f.MediaFiles)
            .HasForeignKey(f => f.LibraryFolderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
