using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  Result<Player> CreateNewPlayer(string playerName);
}

public interface IPlayerActions
{
  Task<Result<Game>> StartNewGame(string name);
  Task<Result<Game>> EndGame(long gameId);
  Task<Result<GamePlayer>> InvitePlayerToActiveGame(long gameId, string playerNameToInvite);
  Task<Result<Game>> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots);
}

public partial class Player
{
  internal sealed class  PlayerFactory(IGameDbContext gameDbContext) : IPlayerFactory
  {
    public Result<Player> CreateNewPlayer(string playerName)
    {
      // TODO Add player validations here

      var newPlayer = new Player
      {
        Name = playerName,
      };

      gameDbContext.Add(newPlayer);

      return newPlayer;
    }
  }

  internal sealed class PlayerActions(IGameDbContext gameDbContext,
    IGameScoreCalculator scoreCalculator,
    IPlayerFactory playerFactory,
    TimeProvider timeProvider,
    IGameLicensePlateFactory licensePlateSpotFactory,
    IQueryable<Player> playerQuery)
      : IPlayerActions
  {
    public async Task<Result<Game>> StartNewGame(string name)
    {
      var queryWithIncludes = playerQuery
        .Include(p => p.OwnedGames.Where(g => g.IsActive).Take(1))
        .Include(p => p.GamePlayers.Where(igp => igp.Game.IsActive).Take(1));

      var actingPlayerResult = await GetActingPlayer(queryWithIncludes);
      if (!actingPlayerResult.TryGetSuccessful(out var actingPlayer, out var failure))
      {
        return failure;
      }

      var hasActiveGame = actingPlayer.OwnedGames
        .Concat(actingPlayer.GamePlayers.Select(igp => igp.Game))
        .Any(game => game.IsActive);

      if (hasActiveGame)
      {
        return new Failure(ErrorMessageProvider.AlreadyHasActiveGameError);
      }

      var newGame = new Game
      {
        IsActive = true,
        Name = string.IsNullOrWhiteSpace(name) ?
          DateTimeOffset.UtcNow.ToString("o") :
          name,
        CreatedBy = actingPlayer,
      };

      gameDbContext.Add(newGame);

      actingPlayer.AddDomainEvent(new NewGameStartedEvent(newGame));

      return newGame;
    }

    public async Task<Result<Game>> EndGame(long gameId)
    {
      var queryWithIncludes = playerQuery
        .Include(p => p.OwnedGames.Where(g => g.Id == gameId))
          .ThenInclude(g => g.GameLicensePlates.OrderByDescending(glp => glp.DateCreated));

      var actingPlayerResult = await GetActingPlayer(queryWithIncludes);
      if (!actingPlayerResult.TryGetSuccessful(out var actingPlayer, out var failure))
      {
        return failure;
      }

      var currentGame = actingPlayer.OwnedGames.FirstOrDefault(game => game.IsActive && game.Id == gameId);
      if (currentGame == null)
      {
        return new Failure(ErrorMessageProvider.InactiveGameInviteError);
      }

      if (currentGame.GameLicensePlates.Count == 0)
      {
        // Remove game altogether if there are no license plates spotted
        ((GameDbContext)gameDbContext).Games.Remove(currentGame);
      }

      var endedOn = currentGame.GameLicensePlates
        .Select(glp => glp.DateCreated)
        .OrderByDescending(dateSpotted => dateSpotted)
        .FirstOrDefault(currentGame.DateCreated);

      currentGame.IsActive = false;
      currentGame.EndedOn = endedOn;

      actingPlayer.AddDomainEvent(new ExistingGameFinishedEvent(currentGame));

      return currentGame;
    }

    public async Task<Result<GamePlayer>> InvitePlayerToActiveGame(long gameId, string playerNameToInvite)
    {
      var queryWithIncludes = playerQuery
        .Include(p => p.OwnedGames.Where(g => g.Id == gameId))
          .ThenInclude(p => p.InvitedPlayers.Where(ip => ip.Name == playerNameToInvite));

      var actingPlayerResult = await GetActingPlayer(queryWithIncludes);
      if (!actingPlayerResult.TryGetSuccessful(out var actingPlayer, out var playerFailure))
      {
        return playerFailure;
      }

      var currentGame = actingPlayer.OwnedGames.FirstOrDefault(game => game.IsActive && game.Id == gameId);
      if (currentGame == null)
      {
        return new Failure(ErrorMessageProvider.InactiveGameInviteError);
      }

      if (currentGame.InvitedPlayers.Any(existingInv => existingInv.Name == playerNameToInvite))
      {
        return new Failure(ErrorMessageProvider.PlayerAlreadyInvitedError);
      }
      var playerToInviteResult = playerFactory.CreateNewPlayer(playerNameToInvite);
      if (!playerToInviteResult.TryGetSuccessful(out var playerToInvite, out var failure))
      {
        return failure;
      }
      var newPlayerInvite = new GamePlayer(playerToInvite);

      currentGame._gamePlayers.Add(newPlayerInvite);

      return newPlayerInvite;
    }

    public async Task<Result<Game>> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots)
    {
      var queryWithIncludes = playerQuery
        .Include(p => p.GamePlayers.Where(gp => gp.GameId == gameId && gp.Game.IsActive))
          .ThenInclude(p => p.Game.GameLicensePlates)
              .ThenInclude(glp => glp.LicensePlate)
        .Include(p => p.OwnedGames.Where(g => g.Id == gameId && g.IsActive))
          .ThenInclude(ig => ig.GameLicensePlates)
            .ThenInclude(glp => glp.LicensePlate);

      var actingPlayerResult = await GetActingPlayer(queryWithIncludes);
      if (!actingPlayerResult.TryGetSuccessful(out var actingPlayer, out var failure))
      {
        return failure;
      }

      var currentGame = actingPlayer.OwnedGames
        .Concat(actingPlayer.GamePlayers.Select(igp => igp.Game))
        .FirstOrDefault(game => game.IsActive && game.Id == gameId);
      
      if (currentGame == null)
      {
        return new Failure(ErrorMessageProvider.InactiveGameError);
      }

      var existingSpotsLookup = currentGame.GameLicensePlates
        .ToDictionary(spot => new LicensePlate.PlateKey(spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince));

      // add new spots
      var newSpots = licensePlateSpots
        .Where(spot => !existingSpotsLookup.ContainsKey(spot))
        .ToList()
        .AsReadOnly();

      foreach (var plateKey in newSpots)
      {
        var newSpotResult = licensePlateSpotFactory.CreateLicensePlateSpot(plateKey,
          actingPlayer,
          timeProvider.GetUtcNow());

        if (!newSpotResult.TryGetSuccessful(out var newSpot, out var spotFailure))
        {
          return spotFailure;
        }

        currentGame._gameLicensePlates.Add(newSpot);
      }

      // remove any existing spots
      var updatedSpotsLookup = licensePlateSpots
        .ToHashSet();

      var spotsToRemove = existingSpotsLookup
        .Where(existingSpot => !updatedSpotsLookup.Contains(existingSpot.Key))
        .Select(existingSpot => existingSpot.Value)
        .ToHashSet();

      currentGame._gameLicensePlates.RemoveWhere(spotsToRemove.Contains);

      // update score and notify players if spots were updated
      if (newSpots.Count != 0 || spotsToRemove.Count != 0)
      {
        var allSpottedPlates = currentGame.GameLicensePlates
          .Select(glp => new LicensePlate.PlateKey(glp.LicensePlate.Country, glp.LicensePlate.StateOrProvince))
          .ToList()
          .AsReadOnly();

        var newScore = scoreCalculator.CalculateGameScore(allSpottedPlates);

        currentGame.GameScore = currentGame.GameScore with
        {
          Achievements = newScore.Achievements.ToList().AsReadOnly(),
          TotalScore = newScore.TotalScore
        };

        actingPlayer.AddDomainEvent(new LicensePlateSpottedEvent(currentGame));
      }

      return currentGame;
    }

    private async Task<Result<Player>> GetActingPlayer(IQueryable<Player> playerQuery)
    {
      var players = await playerQuery.ToArrayAsync();
      
      if (players.Length == 0)
      {
        // TODO need better message here
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }
      else if (players.Length > 1)
      {
        // TODO need better message here
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }
      else
      {
        return players[0];
      }
    }
  }
}
