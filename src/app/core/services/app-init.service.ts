import { Injectable } from '@angular/core';
import { Game } from '../models';

@Injectable()
export class AppInitService {
  private _initData: any;

  constructor() { }

  public get initData(): any {
    return this._initData;
  }

  public loadInitData(data: any): void {
    this._initData = data;
  }
}
