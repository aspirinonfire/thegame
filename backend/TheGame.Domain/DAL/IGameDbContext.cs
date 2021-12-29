using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Domain.DAL
{
  public interface IGameDbContext : IGameUoW
  {
    DbSet<LicensePlate> LicensePlates { get; set; }
    DbSet<Team> Teams { get; set; }
    DbSet<Player> Players { get; set; }
    EntityEntry<T> Entry<T>(T entity) where T : class;
    EntityEntry Add([NotNull] object entity);
  }
}
