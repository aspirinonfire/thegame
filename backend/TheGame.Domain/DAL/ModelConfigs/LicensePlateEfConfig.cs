using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL.ModelConfigs
{
  class LicensePlateEfConfig : IEntityTypeConfiguration<LicensePlateModel>
  {
    public void Configure(EntityTypeBuilder<LicensePlateModel> builder)
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
      builder.HasData(LicensePlateModel.AvailableLicensePlates);
    }
  }
}
