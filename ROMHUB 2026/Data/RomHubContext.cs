using Microsoft.EntityFrameworkCore;
using ROMHub.Models;

namespace ROMHub.Data
{
    public class RomHubContext : DbContext
    {
        public DbSet<Rom> Roms { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            // Use the InMemory provider during debugging to rule out native SQLite issues.
            optionsBuilder.UseInMemoryDatabase("RomHub_InMemory");
#else
            optionsBuilder.UseSqlite("Data Source=romhub.db");
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Allow CoverImagePath to be optional so a ROM can be added without a cover image.
            modelBuilder.Entity<Rom>()
                .Property(r => r.CoverImagePath)
                .IsRequired(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
