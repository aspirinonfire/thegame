using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheGame.Domain.DomainModels.LicensePlates;

class LicensePlateEfConfig : IEntityTypeConfiguration<LicensePlate>
{
  public void Configure(EntityTypeBuilder<LicensePlate> builder)
  {
    builder
      .HasKey(model => model.Id);

    builder
      .Property(model => model.Country)
      .IsRequired()
      .HasConversion<string>();

    builder
      .Property(model => model.StateOrProvince)
      .IsRequired()
      .HasConversion<string>();

    builder
      .HasIndex(plate => new { plate.Country, plate.StateOrProvince })
      .IsUnique();

    // Seed data
    builder.HasData(LicensePlate.AvailableLicensePlates);
  }
}
