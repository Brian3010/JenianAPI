using Jenian.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jenian.Infrastructure.Persistence.Auth
{
  /* Add DbContext so Entity Framework Core (EF Core) can:
   *  know what tables to create in the database, in this case this class inherits from
   *    identityDbContext<IdentityUser>.
   * 
   *  Track and save custom data if has one (e.g., Shifts, Expenses)
   *  Work with ASP.NET Identity tables
   * 
   */
  public class JenianAuthDbContext : IdentityDbContext<ApplicationUser>

  {

    public JenianAuthDbContext(DbContextOptions<JenianAuthDbContext> options) : base(options) {

    }


    // Create tables
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ApplicationUser> ApplicationUser { get; set; }


    protected override void OnModelCreating(ModelBuilder builder) {
      base.OnModelCreating(builder);

      builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);


      // using index to help clean up expired demo users efficiently
      builder.Entity<ApplicationUser>()
            .HasIndex(user => new {
              user.IsDemoUser,
              user.DemoExpiresAtUtc
            });


    }


  }
}
