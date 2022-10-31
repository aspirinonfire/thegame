import { Injectable } from '@angular/core';
import { Game, LicensePlate, Country, ScoreData, StateBorder } from '../models'
import { LocalStorageService } from './local-storage.service';
import { mockGameData, UsStateBorders } from '../mockData';

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private readonly CURRENT_GAME_KEY = "currentGame";
  private readonly PAST_GAMES_KEY = "pastGames";
  private static territoryNameLkp: ReadonlyMap<string, string>;

  private static westCoastUsStatesLkp = mockGameData
    .filter(state => !!state.modifier && state.modifier.indexOf('West Coast') >= 0)
    .map(state => state.shortName)
    .reduce((lkp, state) => lkp.add(state), new Set<string>());

  private static eastCoastStatesLkp = mockGameData
    .filter(state => !!state.modifier && state.modifier?.indexOf('East Coast') >= 0)
    .map(state => state.shortName)
    .reduce((lkp, state) => lkp.add(state), new Set<string>());

  public static ScopeMultiplierByPlateLkp: ReadonlyMap<string, number>;

  constructor(private readonly storageSvc: LocalStorageService) {
    if (!GameService.territoryNameLkp) {
      const lkp = new Map<string, string>();
      mockGameData.forEach(t => {
        lkp.set(this.getNameKey(t.shortName, t.country), t.longName);
      });
      GameService.territoryNameLkp = lkp;
    }

    if (!GameService.ScopeMultiplierByPlateLkp)
    {
      const scoreLkp = mockGameData
      .reduce((lkp, territory) => {
        lkp.set(`${territory.country}-${territory.shortName}`, territory.scoreMultiplier ?? 1);
        return lkp;
      }, new Map<string, number>());
      GameService.ScopeMultiplierByPlateLkp = scoreLkp;
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
      name: name,
      score: <ScoreData>{
        totalScore: 0,
        milestones: []
      }
    };
    this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    return currentGame;
  }

  public finishActiveGame(): string | null {
    const currentGame = this.getCurrentGame();
    if (!currentGame) {
      return "No active game!";
    }

    const lastSpot = Object.keys(currentGame.licensePlates)
      .map(key => currentGame.licensePlates[key].dateSpotted)
      .sort()
      .reverse()[0];

    currentGame.dateFinished = lastSpot ?? new Date();
    
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
    } else {
      const licensePlate = <LicensePlate>{
        dateSpotted: new Date(),
        spottedBy: spottedBy,
        stateOrProvince: stateOrProvice,
        country: country
      };
      currentGame.licensePlates[key] = licensePlate;
    }

    this.calculateScore(currentGame);
    this.storageSvc.setValue(this.CURRENT_GAME_KEY, currentGame);
    
    return currentGame.licensePlates;
  }

  resetAll() {
    this.storageSvc.clearAll();
  }

  private calculateScore(currentGame: Game): void
  {
    // recalculate all scores
    currentGame.score = <ScoreData>{
      totalScore: 0,
      milestones: []
    };

    const spottedPlates = Object.keys(currentGame.licensePlates);

    let score = spottedPlates
      .reduce((currentScore, key) =>{
        const multiplier = GameService.ScopeMultiplierByPlateLkp.get(key) ?? 1;

        return currentScore + multiplier;
      }, 0);

    if (this.hasWestCoast(currentGame.licensePlates))
    {
      score += 10;
      currentGame.score.milestones.push('West Coast');
    }

    if (this.hasEastCoast(currentGame.licensePlates))
    {
      score += 20;
      currentGame.score.milestones.push('East Coast');
    }

    if (this.hasTransAtlantic(currentGame.licensePlates))
    {
      score += 100;
      currentGame.score.milestones.push('Coast-to-Coast');
    }

    currentGame.score.totalScore = score;
  }

  private hasWestCoast(plates: {[key: string]: LicensePlate}) : boolean {
    const markedUsStates = Object.keys(plates)
      .map(key => plates[key])
      .filter(plate => plate.country == 'US')
      .map(plate => plate.stateOrProvince);

    return markedUsStates
      .filter(state => GameService.westCoastUsStatesLkp.has(state))
      .length == GameService.westCoastUsStatesLkp.size;
  }

  private hasEastCoast(plates: {[key: string]: LicensePlate}) : boolean {
    const markedUsStates = Object.keys(plates)
      .map(key => plates[key])
      .filter(plate => plate.country == 'US')
      .map(plate => plate.stateOrProvince);

    return markedUsStates
      .filter(state => GameService.eastCoastStatesLkp.has(state))
      .length == GameService.eastCoastStatesLkp.size;
  }

  private hasTransAtlantic(plates: {[key: string]: LicensePlate}) : boolean {
    const markedUsStates = Object.keys(plates)
      .map(key => plates[key])
      .filter(plate => plate.country == 'US')
      .map(plate => plate.stateOrProvince);

    const markedWestCoastStates = markedUsStates
      .filter(state => GameService.westCoastUsStatesLkp.has(state));

    return this.isConnected(markedWestCoastStates,
       (state: string) => GameService.eastCoastStatesLkp.has(state),
       markedUsStates.map(state => state));
  }

  private isConnected(startingStates: string[],
    isMatchingBorderState: (state: string) => boolean,
    markedStates: string[]): boolean {
    
    const toCheckLkp = startingStates
      .reduce((lkp, state) => lkp.add(state), new Set<string>());

    const markedStatesLkp = markedStates
      .reduce((lkp, state) => lkp.add(state), new Set<string>())
    
    const checkedStatesLkp = new Set<string>();

    while (toCheckLkp.size > 0)
    {
      const [first] = toCheckLkp;
      checkedStatesLkp.add(first);
      toCheckLkp.delete(first);
      const currentStateBorders = UsStateBorders[first] ?? <StateBorder>{};
      const currentMarkedBorders = Object.keys(currentStateBorders)
        // ensure border state has been marked, and hasn't been visited or been marked for visiting
        .filter((borderState : string) => markedStatesLkp.has(borderState) &&
          !toCheckLkp.has(borderState) &&
          !checkedStatesLkp.has(borderState));

      for (const borderingState of currentMarkedBorders)
      {
          if (isMatchingBorderState(borderingState))
          {
              return true;
          }
          else
          {
              toCheckLkp.add(borderingState);
          }
      }
    }

    return false;
  }
}
