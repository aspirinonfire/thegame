using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL.ModelConfigs
{
  class GameEfConfig : IEntityTypeConfiguration<Game>
  {
    public void Configure(EntityTypeBuilder<Game> builder)
    {
      builder.ToTable("Games");

      // define properties
      builder
        .HasKey(game => game.Id);

      builder
        .Property(game => game.Name);

      builder
        .Property(game => game.IsActive);

      builder
        .Property(game => game.EndedOn)
        .IsRequired(false);

      // define relationships
      builder
        .HasMany(game => game.LicensePlates)
        .WithMany(licensePlate => licensePlate.Games)
        .UsingEntity<GameLicensePlate>("GameLicensePlates",
          j => j
            .HasOne(glp => glp.LicensePlate)
            .WithMany(plate => plate.GameLicensePlates)
            .HasForeignKey(glp => glp.LicensePlateId),
          j => j
            .HasOne(glp => glp.Game)
            .WithMany(game => game.GameLicensePlates)
            .HasForeignKey(glp => glp.GameId),
          j =>
          {
            j.HasOne(glp => glp.SpottedBy).WithMany().IsRequired();
            j.Property(glp => glp.DateCreated);
            j.HasKey(glp => new { glp.GameId, glp.LicensePlateId });
          });
    }
  }
}
