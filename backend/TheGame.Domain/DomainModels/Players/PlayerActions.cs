using System;
using System.Collections.Generic;
using System.Linq;
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
  Player Player { get; }
  Result<Game> StartNewGame(string name);
  Result<Game> EndGame(long gameId);
  Result<GamePlayer> InvitePlayerToActiveGame(long gameId, string playerNameToInvite);
  Result<Game> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots);
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
    Player actingPlayer)
      : IPlayerActions
  {
    public Player Player => actingPlayer;

    public Result<Game> StartNewGame(string name)
    {
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

      gameDbContext.Add(newGame);

      actingPlayer.AddDomainEvent(new NewGameStartedEvent(newGame));

      return newGame;
    }

    public Result<Game> EndGame(long gameId)
    {
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

    public Result<GamePlayer> InvitePlayerToActiveGame(long gameId, string playerNameToInvite)
    {
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

      currentGame._gamePlayerInvites.Add(newPlayerInvite);

      return newPlayerInvite;
    }

    public Result<Game> UpdateLicensePlateSpots(long gameId, IReadOnlyCollection<LicensePlate.PlateKey> licensePlateSpots)
    {
      var currentGame = actingPlayer.OwnedGames
        .Concat(actingPlayer.InvitedGames)
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
  }
}
