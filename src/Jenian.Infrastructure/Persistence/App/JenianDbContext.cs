using Jenian.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.App
{
  public class JenianDbContext : DbContext
  {

    public JenianDbContext(DbContextOptions<JenianDbContext> dbContextOptions) : base(dbContextOptions)
    {

    }


    // Create tables
    public DbSet<DeliveryExtractionJob> DeliveryExtractionJobs { get; set; }
    public DbSet<EodReport> EodReports { get; set; }

    public DbSet<UserShift> UserShifts { get; set; }
    public DbSet<UserDailyPaySummary> UserDailyPaySummaries { get; set; }



    protected override void OnModelCreating(ModelBuilder builder)
    {
      builder.Entity<EodReport>().OwnsOne(e => e.StockUpdate);
      builder.Entity<EodReport>().OwnsOne(e => e.NightTasks);
      builder.Entity<EodReport>().OwnsOne(e => e.AislesFacing);
      builder.Entity<EodReport>().OwnsOne(e => e.Cleaning);
      builder.Entity<EodReport>().OwnsOne(e => e.GeneralCheck);

      builder.Entity<UserDailyPaySummary>()
        .HasMany(d => d.Shifts)
        .WithOne(s => s.UserDailyPaySummary)
        .HasForeignKey(s => s.UserDailyPaySummaryId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.SetNull);

      builder.Entity<UserDailyPaySummary>()
        .HasIndex(x => new { x.UserId, x.WorkDate })
        .IsUnique();

      base.OnModelCreating(builder);
    }

  }
}
