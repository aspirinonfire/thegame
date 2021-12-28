using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL.ModelConfigs
{
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

      // Seed data
      builder.HasData(LicensePlate.AvailableLicensePlates);
    }
  }
}
