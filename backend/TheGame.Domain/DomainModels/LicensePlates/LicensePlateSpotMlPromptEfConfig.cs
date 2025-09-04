using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TheGame.Domain.DomainModels.LicensePlates;

internal sealed class LicensePlateSpotMlPromptEfConfig : IEntityTypeConfiguration<LicensePlateSpotMlPrompt>
{
  public void Configure(EntityTypeBuilder<LicensePlateSpotMlPrompt> builder)
  {
    builder.ToTable("LicensePlateSpotMlPrompts");
    builder.HasKey(p => p.Id);

    builder.Property(p => p.MlPrompt)
      .IsRequired();

    builder.Property(p => p.GameId)
      .IsRequired();

    builder.Property(p => p.SpottedByPlayerId)
      .IsRequired();

    builder.HasOne(p => p.LicensePlate)
      .WithMany()
      .HasForeignKey(p => p.LicensePlateId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(p => p.Game)
      .WithMany()
      .HasForeignKey(p => p.GameId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasOne(p => p.SpottedBy)
      .WithMany()
      .HasForeignKey(p => p.SpottedByPlayerId)
      .OnDelete(DeleteBehavior.Restrict);

    builder.HasIndex(p => new { p.GameId, p.LicensePlateId });
  }
}
