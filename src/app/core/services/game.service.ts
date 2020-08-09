import { Injectable } from '@angular/core';
import { Game, UsStates, CanadaProvinces, LicensePlate } from '../models'
import { LocalStorageService } from './local-storage.service';
import { AppInitService } from './app-init.service';

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private readonly CURRENT_GAME_KEY = "currentGame";
  private readonly PAST_GAMES_KEY = "pastGames";

  constructor(private readonly storageSvc: LocalStorageService) { }

  public getCurrentGame(): Game | null {
    return this.storageSvc.getValue<Game>(this.CURRENT_GAME_KEY);
  }

  public createGame(name: string, createdBy: string): Game {
    let currentGame = this.getCurrentGame();
    if (!!currentGame) {
      const pastGames = this.getPastGames();
      pastGames.push(currentGame);
      currentGame.dateFinished = new Date();
      this.storageSvc.setValue(this.PAST_GAMES_KEY, pastGames);
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

  public saveSpottedPlate(plate: UsStates | CanadaProvinces, spottedBy: string): string | { [K: string]: LicensePlate } {
    const currentGame = this.getCurrentGame();
    if (!currentGame) {
      return "No active game was found!";
    }

    if (!!currentGame.licensePlates[plate]) {
      delete currentGame.licensePlates[plate];
      this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    } else {
      const licensePlate = {
        dateSpotted: new Date(),
        spottedBy: spottedBy,
        stateOrProvice: plate
      }
      currentGame.licensePlates[plate] = licensePlate;
      this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    }
    
    return currentGame.licensePlates;
  }

  resetAll() {
    this.storageSvc.clearAll();
  }
}
