using Claims.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;


namespace Claims.Data;

public class ClaimsContext : DbContext
{
    public DbSet<Claim> Claims { get; set; } = null!;
    public DbSet<Cover> Covers { get; set; } = null!;

    public ClaimsContext(DbContextOptions<ClaimsContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>().ToCollection("claims");
        modelBuilder.Entity<Cover>().ToCollection("covers");
    }
}