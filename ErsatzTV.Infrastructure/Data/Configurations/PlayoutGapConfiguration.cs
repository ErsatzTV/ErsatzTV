using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class PlayoutGapConfiguration : IEntityTypeConfiguration<PlayoutGap>

{
    public void Configure(EntityTypeBuilder<PlayoutGap> builder)
    {
        builder.ToTable("PlayoutGap");

        builder.HasIndex(p => new { p.Start, p.Finish })
            .HasDatabaseName("IX_PlayoutGap_Start_Finish");
    }
}
