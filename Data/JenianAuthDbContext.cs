using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JenianAPI.Data
{
  /* Add DbContext so Entity Framework Core (EF Core) can:
   *  know what tables to create in the database, in this case this class inherits from
   *    identityDbContext<IdentityUser>.
   * 
   *  Track and save custom data if has one (e.g., Shifts, Expenses)
   *  Work with ASP.NET Identity tables
   * 
   */
  public class JenianAuthDbContext : IdentityDbContext<IdentityUser>

  {

    public JenianAuthDbContext(DbContextOptions<JenianAuthDbContext> options) : base(options) {

    }
  }
}
