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

public interface IPlayerActions
{
  Result<Player> CreateNewPlayer(string playerName);
  Task<Result<Game>> StartNewGame(string name);
  Task<Result<Game>> EndGame(long gameId);
  Task<Result<GamePlayer>> InvitePlayerToActiveGame(long gameId, string playerNameToInvite);
  Task<Result<Game>> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots);
}

public partial class Player
{
  internal sealed class PlayerActions(IGameDbContext gameDbContext,
    IGameScoreCalculator scoreCalculator,
    TimeProvider timeProvider,
    IGameLicensePlateFactory licensePlateSpotFactory,
    long actingPlayerId)
      : IPlayerActions
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

    public async Task<Result<Game>> StartNewGame(string name)
    {
      var actingPlayer = await gameDbContext.Players
        .Include(p => p.OwnedGames)
        .Include(p => p.InvitedGames)
        .Where(p => p.Id == actingPlayerId)
        .FirstOrDefaultAsync();

      if (actingPlayer == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }

      var hasActiveGame = actingPlayer.OwnedGames
        .Concat(actingPlayer.InvitedGames)
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

      gameDbContext.Games.Add(newGame);

      actingPlayer.AddDomainEvent(new NewGameStartedEvent(newGame));

      return newGame;
    }

    public async Task<Result<Game>> EndGame(long gameId)
    {
      var actingPlayer = await gameDbContext.Players
        .Include(p => p.OwnedGames)
        .Where(p => p.Id == actingPlayerId)
        .FirstOrDefaultAsync();

      if (actingPlayer == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }
      
      var currentGame = actingPlayer.OwnedGames.FirstOrDefault(game => game.IsActive && game.Id == gameId);
      if (currentGame == null)
      {
        return new Failure(ErrorMessageProvider.InactiveGameInviteError);
      }

      if (currentGame.GameLicensePlates.Count == 0)
      {
        gameDbContext.Games.Remove(currentGame);
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
      var actingPlayer = await gameDbContext.Players
        .Include(p => p.OwnedGames)
          .ThenInclude(g => g.InvitedPlayers)
        .Include(p => p.InvatedGamePlayers)
        .Where(p => p.Id == actingPlayerId)
        .FirstOrDefaultAsync();

      if (actingPlayer == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
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
      var playerToInviteResult = CreateNewPlayer(playerNameToInvite);
      if (!playerToInviteResult.TryGetSuccessful(out var playerToInvite, out var failure))
      {
        return failure;
      }
      var newPlayerInvite = new GamePlayer(playerToInvite);

      currentGame._gamePlayerInvites.Add(newPlayerInvite);

      return newPlayerInvite;
    }

    public async Task<Result<Game>> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots)
    {
      var actingPlayer = await gameDbContext.Players
        .Include(p => p.OwnedGames)
          .ThenInclude(g => g.GameLicensePlates)
            .ThenInclude(g => g.LicensePlate)
        .Include(p => p.InvitedGames)
          .ThenInclude(g => g.GameLicensePlates)
            .ThenInclude(g => g.LicensePlate)
        .Where(p => p.Id == actingPlayerId)
        .FirstOrDefaultAsync();

      if (actingPlayer == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }

      var currentGame = actingPlayer.OwnedGames
        .Concat(actingPlayer.InvitedGames)
        .FirstOrDefault(game => game.IsActive && game.Id == gameId);
      
      if (currentGame == null)
      {
        return new Failure(ErrorMessageProvider.InactiveGameInviteError);
      }

      var existingSpotsLookup = currentGame.GameLicensePlates
        .ToDictionary(spot => new LicensePlate.PlateKey(spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince));

      // add new spots
      var newSpots = licensePlateSpots
        .Where(spot => !existingSpotsLookup.ContainsKey(spot))
        .ToList()
        .AsReadOnly();

      var gameDbInstance = gameDbContext as GameDbContext;

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
  }
}
