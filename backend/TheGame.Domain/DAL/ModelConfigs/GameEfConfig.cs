using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL.ModelConfigs;

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

    // define CreatedBy nav props
    builder
      .HasOne(game => game.CreatedBy)
      .WithMany()
      .IsRequired()
      .HasForeignKey("CreatedByPlayerId")
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Navigation(game => game.CreatedBy)
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    // define game license plate nav props
    builder
      .HasMany(game => game.LicensePlates)
      .WithMany(licensePlate => licensePlate.Games)
      .UsingEntity<GameLicensePlate>("GameLicensePlates",
        right => right
          .HasOne(glp => glp.LicensePlate)
          .WithMany(plate => plate.GameLicensePlates)
          .HasForeignKey(glp => glp.LicensePlateId),
        left => left
          .HasOne(glp => glp.Game)
          .WithMany(game => game.GameLicensePlates)
          .HasForeignKey(glp => glp.GameId),
        joinEntity =>
        {
          joinEntity.HasKey(glp => new { glp.GameId, glp.LicensePlateId });

          joinEntity
            .HasOne(glp => glp.SpottedBy)
            .WithMany()
            .HasForeignKey("SpottedByPlayerId")
            .OnDelete(DeleteBehavior.NoAction);

          joinEntity
            .Property(glp => glp.DateCreated)
            .IsRequired();
        });

    var gameLicensePlateNav = builder.Navigation(game => game.GameLicensePlates);
    gameLicensePlateNav
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_gameLicensePlates");

    // define game player nav props
    builder
      .HasMany(game => game.InvitedPlayers)
      .WithMany(player => player.InvitedGames)
      .UsingEntity<GamePlayer>(
        joinEntity =>
        {
          joinEntity.HasKey(gp => new { gp.GameId, gp.PlayerId });

          joinEntity
            .Property(e => e.InvitationToken)
            .IsRequired();

          joinEntity
            .Property(e => e.InviteStatus)
            .IsRequired()
            .HasConversion<string>();

          joinEntity
                .HasOne(gp => gp.Player)
                .WithMany(player => player.InvatedGamePlayers)
                .OnDelete(DeleteBehavior.Restrict);

          joinEntity
              .HasOne(gp => gp.Game)
              .WithMany(game => game.GamePlayerInvites)
              .OnDelete(DeleteBehavior.Restrict);
        });

    var gamePlayerNav = builder.Navigation(game => game.GamePlayerInvites);
    gamePlayerNav
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_gamePlayerInvites");
  }
}
