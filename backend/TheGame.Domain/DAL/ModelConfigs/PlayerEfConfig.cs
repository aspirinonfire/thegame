using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DAL.ModelConfigs
{
  class PlayerEfConfig : IEntityTypeConfiguration<Player>
  {
    public void Configure(EntityTypeBuilder<Player> builder)
    {
      // TODO no autogeneration. Must be set to unique identity id
      builder
        .HasKey(player => player.UserId);

      builder
        .Property(player => player.Name)
        .IsRequired();

      builder
        .Navigation(player => player.Teams)
        .UsePropertyAccessMode(PropertyAccessMode.Field)
        .HasField("_teams");
    }
  }
}
