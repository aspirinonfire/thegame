using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.Games;

namespace TheGame.Domain.DomainModels.Players;

class PlayerEfConfig : IEntityTypeConfiguration<Player>
{
  public void Configure(EntityTypeBuilder<Player> builder)
  {
    builder.ToTable("Players");

    builder.HasKey(player => player.Id);

    builder
      .Property(player => player.Name)
      .IsRequired();

    builder
      .Navigation(player => player.GamePlayers)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_gamePlayers");

    builder
      .HasMany(player => player.OwnedGames)
      .WithOne(game => game.CreatedBy)
      .HasForeignKey(game => game.CreatedByPlayerId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Navigation(player => player.OwnedGames)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_ownedGames");

    // starting from dependent entity rather than principal (PlayerIdentity) to better separate concerns.
    // player can be invited before signing up so identity is not guaratneed.
    builder
      .HasOne(player => player.PlayerIdentity)
      .WithOne(identity => identity.Player)
      .IsRequired(false)
      .OnDelete(DeleteBehavior.Cascade);

    // define game player nav props
    builder
      .HasMany(player => player.InvitedGames)
      .WithMany(game => game.InvitedPlayers)
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
            .WithMany(player => player.GamePlayers)
            .OnDelete(DeleteBehavior.Restrict);

          joinEntity
            .HasOne(gp => gp.Game)
            .WithMany(game => game.GamePlayers)
            .OnDelete(DeleteBehavior.Restrict);
        });
  }
}
