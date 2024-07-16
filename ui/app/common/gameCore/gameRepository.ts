import { Game, ScoreData, LicensePlateSpot, Territory, NewSpottedPlate } from "./gameModels";
import { mockGameData } from "@/app/common/data/mockGameData";
import { GetFromLocalStorage, SendAuthenticatedApiRequest, SetLocalStorage } from "@/app/appUtils";
import { PlayerInfo } from "../accounts";

const currentGameKey: string = "currentGame";

export async function GetCurrentGame() : Promise<Game | null> {
  return GetFromLocalStorage(currentGameKey);
}

export async function RetrieveActiveGame() : Promise<Game | null> {
  const activeGames = await SendAuthenticatedApiRequest<Game[]>("game?isActive=true", "GET");
  const currentActiveGame = activeGames?.at(0) ?? null;

  SetLocalStorage(currentGameKey, currentActiveGame);

  return currentActiveGame;
}

export async function GetPastGames(): Promise<Game[]> {
  const allGames = await SendAuthenticatedApiRequest<Game[]>("game", "GET") ?? [];

  return allGames
    .filter(game => !!game.endedOn)
}

export async function CreateNewGame(name: string): Promise<Game | string> {
  let currentGame = await GetCurrentGame();
  if (!!currentGame) {
    return "Only one active game is allowed!";
  }

  const newGameRequest = {
    newGameName: name
  };

  const newGame = await SendAuthenticatedApiRequest<Game>("game", "POST", newGameRequest);
  if (!newGame) {
    return "Failed to create new game.";
  }

  SetLocalStorage(currentGameKey, newGame);
  
  return newGame;
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

  const endedGame = await SendAuthenticatedApiRequest<Game>(`game/${currentGame.gameId}/endgame`, "POST");
  if (!endedGame) {
    return "Failed to save updated plate spots.";
  }

  SetLocalStorage(currentGameKey, null);

  return null;
}

export async function UpdateCurrentGameWithNewSpots(newSpottedPlates: NewSpottedPlate[]): Promise<Game | string> {
  const currentGame = await GetCurrentGame();
  if (!currentGame) {
    return "No active game!";
  }

  const updatedGame = await SendAuthenticatedApiRequest<Game>(`game/${currentGame.gameId}/spotplates`, "POST", newSpottedPlates);
  if (!updatedGame) {
    return "Failed to save updated plate spots.";
  }

  SetLocalStorage(currentGameKey, updatedGame);

  return updatedGame;
}

export async function GetAccount() : Promise<PlayerInfo | null> {
  return await SendAuthenticatedApiRequest("user", "GET");
}