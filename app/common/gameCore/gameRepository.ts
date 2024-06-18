import { Game, ScoreData, LicensePlateSpot } from "./gameModels";
import CalculateScore from "./GameScoreCalculator";
import UserAccount from "../accounts";
import { mockGameData } from "../data/mockGameData";

const currentGameKey: string = "currentGame";
const pastGamesKey: string = "pastGames";

async function mockDataAccessDelay() : Promise<void> {
  return new Promise(resolve => {
    setTimeout(() => {
      resolve();
    }, 200);
  })
}

function GetFromLocalStorage<T>(key: string) : T | null {
  const rawValue = localStorage.getItem(key);
  if (!rawValue) {
    return null;
  }

  try {
    return JSON.parse(rawValue) as T;
  }
  catch (jsonException) {
    console.error(`Failed to retrieve ${key} from local storage:`, jsonException);
    return null;
  }
}

function SetLocalStorage(key: string, value: any): void {
  localStorage.setItem(key, JSON.stringify(value));
}

export async function GetCurrentGame() : Promise<Game | null> {
  await mockDataAccessDelay();
  
  return GetFromLocalStorage(currentGameKey);
}

export async function GetPastGames(): Promise<Game[]> {
  await mockDataAccessDelay();
  
  return GetFromLocalStorage(pastGamesKey) ?? [];
}

export async function CreateNewGame(name: string, createdBy: string): Promise<Game | string> {
  let currentGame = await GetCurrentGame();
  if (!!currentGame) {
    return "Only one active game is allowed!";
  }

  const initialSpots = mockGameData
    .map(territory => {
      return {
        stateOrProvince: territory.shortName,
        country: territory.country,
        fullName: territory.longName,
        plateKey: `${territory.country}_${territory.shortName}`.toLowerCase(),
        dateSpotted: null,
        spottedBy: null,
        plateImageUrl: `./plates/${territory.country}-${territory.shortName}.jpg`.toLowerCase()
      } as LicensePlateSpot
    })
    .reduce((allSpots, plate) => {
      allSpots[plate.plateKey] = plate;
      return allSpots;
    }, {} as { [key: string]: LicensePlateSpot });

  currentGame = <Game>{
    dateCreated: new Date(),
    createdBy: createdBy,
    id: new Date().getTime().toString(),
    licensePlates: initialSpots,
    name: name,
    score: <ScoreData>{
      totalScore: 0,
      milestones: []
    }
  };

  SetLocalStorage(currentGameKey, currentGame);
  
  return currentGame;
}

export async function FinishActiveGame(): Promise<string | null> {
  const currentGame = await GetCurrentGame();
  if (!currentGame) {
    return "No active game!";
  }

  // use last spot as date finished
  const lastSpot = Object.keys(currentGame.licensePlates)
    .map(key => currentGame.licensePlates[key].dateSpotted)
    .filter(date => !!date)
    .sort()
    .at(-1);

  if (!!lastSpot) {
    currentGame.dateFinished = lastSpot;
  
    const pastGames = await GetPastGames();
    pastGames.push(currentGame);
  
    SetLocalStorage(pastGamesKey, pastGames);
  }

  SetLocalStorage(currentGameKey, null);

  return null;
}

export async function UpdateCurrentGameWithNewSpots(newPlateSpotsLkp: { [key: string]: LicensePlateSpot }): Promise<Game | string> {
  const currentGame = await GetCurrentGame();
  if (!currentGame) {
    return "No active game!";
  }

  const plateSpotCalcInput = Object.keys(newPlateSpotsLkp)
    .map(key => newPlateSpotsLkp[key]);


  const updatedGame = <Game>{...currentGame,
    licensePlates: newPlateSpotsLkp,
    score: CalculateScore(plateSpotCalcInput)
  };

  SetLocalStorage(currentGameKey, updatedGame);

  return updatedGame;
}

export async function GetAccount() : Promise<UserAccount> {
  await mockDataAccessDelay();

  return {
    name: 'Alex'
  }
}