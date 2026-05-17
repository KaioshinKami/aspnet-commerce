using Microsoft.EntityFrameworkCore;
using VehicleService.Domain.Entities;

namespace VehicleService.Infrastructure.Persistence;

public class VehicleDbContext : DbContext
{
    public VehicleDbContext(DbContextOptions<VehicleDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Brand).IsRequired().HasMaxLength(100);
            e.Property(v => v.Model).IsRequired().HasMaxLength(100);
            e.Property(v => v.PricePerDay).HasColumnType("decimal(10,2)");
        });
    }
}
