using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheGame.Domain.DomainModels.Players;

class PlayerEfConfig : IEntityTypeConfiguration<Player>
{
  public void Configure(EntityTypeBuilder<Player> builder)
  {
    builder
      .HasKey(player => player.Id);

    builder
      .Property(player => player.Id)
      .ValueGeneratedNever();

    builder
      .Property(player => player.Name)
      .IsRequired();

    builder
      .Navigation(player => player.InvatedGamePlayers)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .HasField("_invitedGamePlayers");

    // starting from dependent entity rather than principal (PlayerIdentity) to better separate concerns.
    builder
      .HasOne(player => player.PlayerIdentity)
      .WithOne(identity => identity.Player)
      .IsRequired(true)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
