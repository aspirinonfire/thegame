import { Injectable } from '@angular/core';
import { Territory, UserAccount } from '../models';

@Injectable()
export class AppInitDataService {
  private _gameData: Map<string, Territory>;
  private _account: UserAccount;

  constructor() {
    this._gameData = new Map<string, Territory>();
    this._account = <UserAccount>{};
  }

  public get gameData(): ReadonlyMap<string, Territory> {
    return this._gameData;
  }

  public get account() {
    return this._account;
  }

  public loadInitData(account: UserAccount, gameData: Territory[]): void {
    this._account = account;
    for (const data of gameData) {
      this._gameData.set(data.shortName, data);
    }
  }
}
