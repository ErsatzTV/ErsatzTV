using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class TraktListItemGuidConfiguration : IEntityTypeConfiguration<TraktListItemGuid>
{
    public void Configure(EntityTypeBuilder<TraktListItemGuid> builder) => builder.ToTable("TraktListItemGuid");
}