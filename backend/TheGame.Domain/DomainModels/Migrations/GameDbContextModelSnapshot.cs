﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TheGame.Domain.DomainModels;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    [DbContext(typeof(GameDbContext))]
    partial class GameDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("GameLicensePlates", b =>
                {
                    b.Property<long>("GameId")
                        .HasColumnType("bigint");

                    b.Property<long>("LicensePlateId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("DateCreated")
                        .HasColumnType("datetimeoffset");

                    b.Property<long>("SpottedByPlayerId")
                        .HasColumnType("bigint");

                    b.HasKey("GameId", "LicensePlateId");

                    b.HasIndex("LicensePlateId");

                    b.HasIndex("SpottedByPlayerId");

                    b.ToTable("GameLicensePlates");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Games.Game", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("CreatedByPlayerId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("DateCreated")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("DateModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("EndedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedByPlayerId");

                    b.ToTable("Games", (string)null);
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Games.GamePlayer", b =>
                {
                    b.Property<long>("GameId")
                        .HasColumnType("bigint");

                    b.Property<long>("PlayerId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("InvitationToken")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("InviteStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("GameId", "PlayerId");

                    b.HasIndex("PlayerId");

                    b.ToTable("GamePlayer");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.LicensePlates.LicensePlate", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("StateOrProvince")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Country", "StateOrProvince")
                        .IsUnique();

                    b.ToTable("LicensePlates");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            Country = "CA",
                            StateOrProvince = "BC"
                        },
                        new
                        {
                            Id = 2L,
                            Country = "US",
                            StateOrProvince = "AK"
                        },
                        new
                        {
                            Id = 3L,
                            Country = "US",
                            StateOrProvince = "CA"
                        },
                        new
                        {
                            Id = 4L,
                            Country = "US",
                            StateOrProvince = "NV"
                        },
                        new
                        {
                            Id = 5L,
                            Country = "US",
                            StateOrProvince = "OR"
                        },
                        new
                        {
                            Id = 6L,
                            Country = "US",
                            StateOrProvince = "WA"
                        });
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Players.Player", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("PlayerIdentityId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("PlayerIdentityId")
                        .IsUnique();

                    b.ToTable("Players");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Users.PlayerIdentity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<DateTimeOffset>("DateCreated")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("DateModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ProviderIdentityId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderName")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RefreshToken")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ProviderName", "ProviderIdentityId")
                        .IsUnique();

                    b.ToTable("PlayerIdentities", (string)null);
                });

            modelBuilder.Entity("GameLicensePlates", b =>
                {
                    b.HasOne("TheGame.Domain.DomainModels.Games.Game", "Game")
                        .WithMany("GameLicensePlates")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TheGame.Domain.DomainModels.LicensePlates.LicensePlate", "LicensePlate")
                        .WithMany("GameLicensePlates")
                        .HasForeignKey("LicensePlateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TheGame.Domain.DomainModels.Players.Player", "SpottedBy")
                        .WithMany()
                        .HasForeignKey("SpottedByPlayerId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Game");

                    b.Navigation("LicensePlate");

                    b.Navigation("SpottedBy");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Games.Game", b =>
                {
                    b.HasOne("TheGame.Domain.DomainModels.Players.Player", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedByPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Games.GamePlayer", b =>
                {
                    b.HasOne("TheGame.Domain.DomainModels.Games.Game", "Game")
                        .WithMany("GamePlayerInvites")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TheGame.Domain.DomainModels.Players.Player", "Player")
                        .WithMany("InvatedGamePlayers")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Game");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Players.Player", b =>
                {
                    b.HasOne("TheGame.Domain.DomainModels.Users.PlayerIdentity", "PlayerIdentity")
                        .WithOne("Player")
                        .HasForeignKey("TheGame.Domain.DomainModels.Players.Player", "PlayerIdentityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlayerIdentity");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Games.Game", b =>
                {
                    b.Navigation("GameLicensePlates");

                    b.Navigation("GamePlayerInvites");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.LicensePlates.LicensePlate", b =>
                {
                    b.Navigation("GameLicensePlates");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Players.Player", b =>
                {
                    b.Navigation("InvatedGamePlayers");
                });

            modelBuilder.Entity("TheGame.Domain.DomainModels.Users.PlayerIdentity", b =>
                {
                    b.Navigation("Player");
                });
#pragma warning restore 612, 618
        }
    }
}
