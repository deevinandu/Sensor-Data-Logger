using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SensorDataLogger.Models;


namespace SensorDataLogger.Data
{
    public class SensorDbContext : DbContext
    {
        public SensorDbContext(DbContextOptions<SensorDbContext> options)
            : base(options)
        {
        }

        public DbSet<SensorReading> SensorReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SensorReading>()
                .Property(r => r.Timestamp)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
    public class SensorDbContextFactory : IDesignTimeDbContextFactory<SensorDbContext>
    {
        public SensorDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SensorDbContext>();
            optionsBuilder.UseSqlServer("");

            return new SensorDbContext(optionsBuilder.Options);
        }
    }

}
