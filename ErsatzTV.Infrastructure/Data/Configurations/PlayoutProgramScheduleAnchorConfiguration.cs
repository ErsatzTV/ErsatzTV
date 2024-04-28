﻿using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlayoutProgramScheduleAnchorConfiguration : IEntityTypeConfiguration<PlayoutProgramScheduleAnchor>
{
    public void Configure(EntityTypeBuilder<PlayoutProgramScheduleAnchor> builder)
    {
        builder.ToTable("PlayoutProgramScheduleAnchor");

        builder.OwnsOne(a => a.EnumeratorState).ToTable("CollectionEnumeratorState");

        builder.HasOne(i => i.Collection)
            .WithMany()
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MultiCollection)
            .WithMany()
            .HasForeignKey(i => i.MultiCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.SmartCollection)
            .WithMany()
            .HasForeignKey(i => i.SmartCollectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.MediaItem)
            .WithMany()
            .HasForeignKey(i => i.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(i => i.Playlist)
            .WithMany()
            .HasForeignKey(i => i.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.Property(i => i.AnchorDate)
            .IsRequired(false);
    }
}
