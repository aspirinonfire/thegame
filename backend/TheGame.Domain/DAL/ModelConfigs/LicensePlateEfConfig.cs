using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL.ModelConfigs;

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

    // Navigations
    var gameLicensePlateNav = builder.Navigation(plate => plate.GameLicensePlates);
    gameLicensePlateNav
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_gameLicensePlates");

    // Seed data
    builder.HasData(LicensePlate.AvailableLicensePlates);
  }
}
