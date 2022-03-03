using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErsatzTV.Infrastructure.Data.Configurations;

public class LanguageCodeConfiguration : IEntityTypeConfiguration<LanguageCode>
{
    public void Configure(EntityTypeBuilder<LanguageCode> builder) => builder.ToTable("LanguageCode");
}