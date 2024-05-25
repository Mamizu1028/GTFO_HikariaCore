using Hikaria.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hikaria.Core.EntityFramework.Mappings
{
    public class LiveLobbyMap : IEntityTypeConfiguration<LiveLobby>
    {
        public void Configure(EntityTypeBuilder<LiveLobby> builder)
        {
            builder.HasKey(lobby => lobby.LobbyID);

            builder.Property(lobby => lobby.LobbyID)
                .ValueGeneratedNever()
                .HasConversion(p => (long)p, p => (ulong)p);

            builder.Property(lobby => lobby.LobbyName)
                .IsRequired().HasMaxLength(100).ValueGeneratedNever();

            builder.Property(lobby => lobby.ExpirationTime)
                .IsRequired().ValueGeneratedNever();

            builder.OwnsOne(lobby => lobby.PrivacySettings, privacySettings =>
            {
                privacySettings.Property(ps => ps.HasPassword)
                .IsRequired().ValueGeneratedNever();

                privacySettings.Property(ps => ps.Privacy)
                .IsRequired().ValueGeneratedNever();
            });

            builder.OwnsOne(lobby => lobby.DetailedInfo, detailedInfo =>
            {
                detailedInfo.Property(di => di.HostSteamID)
                .IsRequired().ValueGeneratedNever();

                detailedInfo.Property(di => di.Expedition)
                .IsRequired().HasMaxLength(100).ValueGeneratedNever();

                detailedInfo.Property(di => di.ExpeditionName)
                .IsRequired().HasMaxLength(100).ValueGeneratedNever();

                detailedInfo.Property(di => di.OpenSlots)
                .IsRequired().ValueGeneratedNever();

                detailedInfo.Property(di => di.MaxPlayerSlots)
                .IsRequired().ValueGeneratedNever();

                detailedInfo.Property(di => di.RegionName)
                .IsRequired().HasMaxLength(100).ValueGeneratedNever();

                detailedInfo.Property(di => di.Revision)
                .IsRequired().ValueGeneratedNever();

                detailedInfo.Property(di => di.IsPlayingModded)
                .IsRequired().ValueGeneratedNever();

                detailedInfo.Property(di => di.SteamIDsInLobby)
                .ValueGeneratedNever()
                .HasConversion(
                    v => string.Join(',', v.Select(v => v.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToHashSet());
            });

            builder.OwnsOne(lobby => lobby.StatusInfo, statusInfo =>
            {
                statusInfo.Property(si => si.StatusInfo)
                          .IsRequired().HasMaxLength(500).ValueGeneratedNever();
            });

            builder.ToTable("LiveLobbies");
        }
    }
}
