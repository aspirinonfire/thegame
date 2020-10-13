import { Injectable } from '@angular/core';
import { Game, LicensePlate, Country } from '../models'
import { LocalStorageService } from './local-storage.service';
import { mockGameData } from '../mockData';

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private readonly CURRENT_GAME_KEY = "currentGame";
  private readonly PAST_GAMES_KEY = "pastGames";
  private static territoryNameLkp: ReadonlyMap<string, string>;

  constructor(private readonly storageSvc: LocalStorageService) {
    if (!GameService.territoryNameLkp) {
      const lkp = new Map<string, string>();
      mockGameData.forEach(t => {
        lkp.set(this.getNameKey(t.shortName, t.country), t.longName);
      });
      GameService.territoryNameLkp = lkp;
    }
  }

  public getNameKey(stateOrProvice: string, country: string): string {
    return `${country}_${stateOrProvice}`;
  }

  public getLongName(stateOrProvice: string, country: string): string {
    const key = this.getNameKey(stateOrProvice, country);
    return GameService.territoryNameLkp.get(key) || stateOrProvice;
  }

  public getCurrentGame(): Game | null {
    return this.storageSvc.getValue<Game>(this.CURRENT_GAME_KEY);
  }

  public createGame(name: string, createdBy: string): Game | string {
    let currentGame = this.getCurrentGame();
    if (!!currentGame) {
      return "Only one active game is allowed!";
    }

    currentGame = <Game>{
      dateCreated: new Date(),
      createdBy: createdBy,
      id: new Date().getTime().toString(),
      licensePlates: {},
      name: name
    };
    this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    return currentGame;
  }

  public finishActiveGame(): string | null {
    const currentGame = this.getCurrentGame();
    if (!currentGame) {
      return "No active game!";
    }
    currentGame.dateFinished = new Date();
    
    const pastGames = this.getPastGames();
    pastGames.push(currentGame);
    this.storageSvc.setValue(this.CURRENT_GAME_KEY, null);
    this.storageSvc.setValue(this.PAST_GAMES_KEY, pastGames);

    return null;
  }

  public getPastGames(): Game[] {
    return this.storageSvc.getValue<Game[]>(this.PAST_GAMES_KEY) || [];
  }

  public saveSpottedPlate(stateOrProvice: string, country: Country, spottedBy: string): string | { [K: string] : LicensePlate } {
    const currentGame = this.getCurrentGame();
    if (!currentGame) {
      return "No active game was found!";
    }
    const key = `${country}-${stateOrProvice}`;

    if (!!currentGame.licensePlates[key]) {
      delete currentGame.licensePlates[key];
      this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    } else {
      const licensePlate = <LicensePlate>{
        dateSpotted: new Date(),
        spottedBy: spottedBy,
        stateOrProvince: stateOrProvice,
        country: country
      };
      currentGame.licensePlates[key] = licensePlate;
      this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    }
    
    return currentGame.licensePlates;
  }

  resetAll() {
    this.storageSvc.clearAll();
  }
}
