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

            builder.Property(lobby => lobby.LobbyName);

            builder.Property(lobby => lobby.ExpirationTime);

            builder.OwnsOne(lobby => lobby.PrivacySettings, privacySettings =>
            {
                privacySettings.Property(ps => ps.HasPassword);

                privacySettings.Property(ps => ps.Privacy);
            });

            builder.OwnsOne(lobby => lobby.DetailedInfo, detailedInfo =>
            {
                detailedInfo.Property(di => di.HostSteamID);

                detailedInfo.Property(di => di.Expedition);

                detailedInfo.Property(di => di.ExpeditionName);

                detailedInfo.Property(di => di.OpenSlots);

                detailedInfo.Property(di => di.MaxPlayerSlots);

                detailedInfo.Property(di => di.RegionName);

                detailedInfo.Property(di => di.Revision);

                detailedInfo.Property(di => di.IsPlayingModded);

                detailedInfo.Property(di => di.SteamIDsInLobby)
                .HasConversion(
                    v => string.Join(',', v.Select(v => v.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToHashSet());
            });

            builder.OwnsOne(lobby => lobby.StatusInfo, statusInfo =>
            {
                statusInfo.Property(si => si.StatusInfo);
            });

            builder.ToTable("LiveLobbies");
        }
    }
}
