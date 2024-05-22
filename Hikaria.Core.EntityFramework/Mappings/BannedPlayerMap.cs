using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Hikaria.Core.Entities;

namespace Hikaria.Core.EntityFramework.Mappings
{
    public class BannedPlayerMap : IEntityTypeConfiguration<BannedPlayer>
    {
        public void Configure(EntityTypeBuilder<BannedPlayer> builder)
        {
            builder.HasKey(p => p.SteamID);
            builder.Property(p => p.SteamID).ValueGeneratedNever()
                .HasConversion(p => (long)p, p => (ulong)p);
            builder.Property(p => p.Name).HasMaxLength(25).ValueGeneratedNever().IsRequired();
            builder.Property(p => p.Reason).HasMaxLength(50).ValueGeneratedNever().IsRequired();
        }
    }
}
