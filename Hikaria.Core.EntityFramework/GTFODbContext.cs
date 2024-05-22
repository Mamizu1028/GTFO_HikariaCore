using Hikaria.Core.Entities;
using Hikaria.Core.EntityFramework.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Hikaria.Core.EntityFramework
{
    public class GTFODbContext : DbContext
    {
        public GTFODbContext(DbContextOptions<GTFODbContext> options) : base(options)
        {
        }

        public DbSet<BannedPlayer> BannedPlayers { get; set; }
        public DbSet<SteamUser> SteamUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BannedPlayerMap());
            modelBuilder.Entity<BannedPlayer>();

            modelBuilder.ApplyConfiguration(new SteamUserMap());
            modelBuilder.Entity<SteamUser>();
        }
    }
}
