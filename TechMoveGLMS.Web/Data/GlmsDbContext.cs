
using Microsoft.EntityFrameworkCore;
using TechMoveGLMS.Web.Models;

namespace TechMoveGLMS.Web.Data;

public class GlmsDbContext : DbContext
{
    public GlmsDbContext(DbContextOptions<GlmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceRequest>()
            .Property(s => s.CostUsd)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ServiceRequest>()
            .Property(s => s.CostZar)
            .HasPrecision(18, 2);
    }
}
