using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        .IsRequired();

      builder
        .Property(model => model.StateOrProvince)
        .IsRequired();
    }
  }
}
