using Microsoft.EntityFrameworkCore;

namespace HomeHeatMap.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CrimeCity> CrimeCities { get; set; }
    }
}