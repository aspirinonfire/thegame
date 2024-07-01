using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DAL;

public interface IGameDbContext : IGameUoW
{
  DbSet<LicensePlate> LicensePlates { get; set; }
  DbSet<Player> Players { get; set; }
  EntityEntry<T> Entry<T>(T entity) where T : class;
  EntityEntry Add(object entity);
}
