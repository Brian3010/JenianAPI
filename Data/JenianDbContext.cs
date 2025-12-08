using JenianAPI.Models.BackgroundJobsModels;
using JenianAPI.Models.JenianModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace JenianAPI.Data
{
  public class JenianDbContext : DbContext
  {

    public JenianDbContext(DbContextOptions<JenianDbContext> dbContextOptions): base(dbContextOptions) {
      
    }


    // Create tables
    public DbSet<DeliveryExtractionJob> DeliveryExtractionJobs { get; set; }
    public DbSet<EodReport> EodReports { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {
      //builder.Entity<EodReport>().OwnsOne(e => e.Delivery);
      builder.Entity<EodReport>().OwnsOne(e => e.StockUpdate);
      builder.Entity<EodReport>().OwnsOne(e => e.NightTasks);
      builder.Entity<EodReport>().OwnsOne(e => e.AislesFacing);
      builder.Entity<EodReport>().OwnsOne(e => e.Cleaning);
      builder.Entity<EodReport>().OwnsOne(e => e.GeneralCheck);


      base.OnModelCreating(builder);
    }

  }
}
