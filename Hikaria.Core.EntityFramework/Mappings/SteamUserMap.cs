using Hikaria.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hikaria.Core.EntityFramework.Mappings
{
    public class SteamUserMap : IEntityTypeConfiguration<SteamUser>
    {
        public void Configure(EntityTypeBuilder<SteamUser> builder)
        {
            builder.HasKey(p => p.SteamID);
            builder.Property(p => p.SteamID).ValueGeneratedNever()
                .HasConversion(p => (long)p, p => (ulong)p);
            builder.Property(p => p.UserName).HasDefaultValue(string.Empty).IsRequired();
            builder.Property(p => p.Password).ValueGeneratedNever().IsRequired();
            builder.Property(p => p.Privilege).HasDefaultValue(UserPrivilege.None).IsRequired()
                .HasConversion(p => p.ToString(), p => Enum.Parse<UserPrivilege>(p));
        }
    }
}
