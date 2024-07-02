using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheGame.Domain.DomainModels.PlayerIdentities
{
  class PlayerIdentityEfConfig : IEntityTypeConfiguration<PlayerIdentity>
  {
    public void Configure(EntityTypeBuilder<PlayerIdentity> builder)
    {
      builder.ToTable("PlayerIdentities");

      builder.HasKey(iden => iden.Id);

      builder
        .HasIndex(iden => new { iden.ProviderName, iden.ProviderIdentityId })
        .IsUnique();

      builder
        .Property(iden => iden.ProviderName)
        .IsRequired();

      builder
        .Property(iden => iden.ProviderIdentityId)
        .IsRequired();

      builder
        .Property(iden => iden.RefreshToken)
        .IsRequired(false);
    }
  }
}
