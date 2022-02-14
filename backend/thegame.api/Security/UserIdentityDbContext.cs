using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheGame.Api.Security.Models;

namespace TheGame.Api.Security
{
  public class AppUserIdentityDbContext : IdentityDbContext<AppUser>
  {
    public AppUserIdentityDbContext(DbContextOptions<AppUserIdentityDbContext> options)
      : base(options)
    {
    }
  }
}
