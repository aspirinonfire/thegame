using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DAL.ModelConfigs;

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
  }
}
