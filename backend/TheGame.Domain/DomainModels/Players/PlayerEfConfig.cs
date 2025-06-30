using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
      .Navigation(player => player.InvatedGamePlayers)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_invitedGamePlayers");

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
  }
}
