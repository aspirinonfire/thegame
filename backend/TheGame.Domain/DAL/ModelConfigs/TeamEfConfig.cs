using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Domain.DAL.ModelConfigs
{
  class TeamEfConfig : IEntityTypeConfiguration<Team>
  {
    public void Configure(EntityTypeBuilder<Team> builder)
    {
      // define properties
      builder
        .HasKey(team => team.Id);

      builder
        .Property(team => team.Name)
        .IsRequired();

      // define relationships
      builder
        .HasMany(team => team.Players)
        .WithMany(player => player.Teams)
        // TODO explicit entity with Audit trail
        .UsingEntity(j => j.ToTable("TeamPlayers"));

      builder
        .HasMany(team => team.Games);

      // define navigation props
      builder
        .Navigation(team => team.Players)
        .UsePropertyAccessMode(PropertyAccessMode.Field)
        .HasField("_players");

      builder
        .Navigation(team => team.Games)
        .UsePropertyAccessMode(PropertyAccessMode.Field)
        .HasField("_games");
    }
  }
}
