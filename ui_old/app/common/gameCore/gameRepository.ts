import { Game, Territory, NewSpottedPlate } from "./gameModels";
import { mockGameData } from "@/app/common/data/mockGameData";
import { getFromLocalStorage, handleApiResponse, sendAuthenticatedApiRequest, setLocalStorage } from "@/app/appUtils";
import { PlayerInfo } from "../accounts";

const currentGameKey: string = "currentGame";

export async function GetCurrentGame() : Promise<Game | null> {
  return getFromLocalStorage(currentGameKey);
}

export async function RetrieveActiveGame() : Promise<Game | null> {
  const getActiveGamesResult = await sendAuthenticatedApiRequest<Game[]>("game?isActive=true", "GET");

  return handleApiResponse(getActiveGamesResult,
    activeGames => {
      const currentActiveGame = activeGames?.at(0) ?? null;
      setLocalStorage(currentGameKey, currentActiveGame);
      return currentActiveGame
    },
    error => null);
}

export async function GetPastGames(): Promise<Game[] | string> {
  const getPastGamesResult = await sendAuthenticatedApiRequest<Game[]>("game", "GET") ?? [];

  return handleApiResponse<Game[], Game[] | string>(getPastGamesResult,
    pastGames => pastGames.filter(game => !!game.endedOn),
    error => "Failed to retrieve past games"
  )
}

export async function CreateNewGame(name: string): Promise<Game | string> {
  const currentGame = await GetCurrentGame();
  if (!!currentGame) {
    return "Only one active game is allowed!";
  }

  const newGameRequest = {
    newGameName: name
  };

  const newGameResult = await sendAuthenticatedApiRequest<Game>("game", "POST", newGameRequest);

  return handleApiResponse<Game, Game | string>(newGameResult,
    newGame => {
      setLocalStorage(currentGameKey, newGame);
      return newGame;
    },
    error => "Failed to create new game.");
}

export function GetPlateDataForRendering() : Territory[] {
  return mockGameData
    .map(territory => {
      return {
        ...territory,
        licensePlateImgs: [
          `./plates/${territory.country}-${territory.shortName}.jpg`.toLowerCase()
        ]
      }
    })
    .sort();
}

export function GetTerritoryKey(territory: Territory) {
  return `${territory.country}-${territory.shortName}`;
}

export async function FinishActiveGame(): Promise<string | null> {
  const currentGame = await GetCurrentGame();
  if (!currentGame) {
    return "No active game!";
  }

  const endedGame = await sendAuthenticatedApiRequest<Game>(`game/${currentGame.gameId}/endgame`, "POST");
  if (!endedGame) {
    return "Failed to save updated plate spots.";
  }

  setLocalStorage(currentGameKey, null);

  return null;
}

export async function UpdateCurrentGameWithNewSpots(newSpottedPlates: NewSpottedPlate[]): Promise<Game | string> {
  const currentGame = await GetCurrentGame();
  if (!currentGame) {
    return "No active game!";
  }

  const updatedGameResult = await sendAuthenticatedApiRequest<Game>(`game/${currentGame.gameId}/spotplates`, "POST", newSpottedPlates);

  return handleApiResponse<Game, Game | string>(updatedGameResult,
    updatedGame => {
      setLocalStorage(currentGameKey, updatedGame);

      return updatedGame;
    },
    error => "Failed to save updated plate spots."
  )
}

export async function GetAccount() : Promise<PlayerInfo | null> {
  const accountResult = await sendAuthenticatedApiRequest<PlayerInfo>("user", "GET");
  return handleApiResponse(accountResult,
    playerInfo => playerInfo,
    error => null
  )
}