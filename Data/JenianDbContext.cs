using JenianAPI.Models.BackgroundJobsModels;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Data
{
  public class JenianDbContext : DbContext
  {

    public JenianDbContext(DbContextOptions<JenianDbContext> dbContextOptions): base(dbContextOptions) {
      
    }


    // Create tables
    public DbSet<DeliveryExtractionJob> DeliveryExtractionJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder) {

      base.OnModelCreating(builder);
    }

  }
}
