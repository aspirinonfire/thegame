import { Component, ElementRef, Input } from '@angular/core';
import { LicensePlate } from 'src/app/core/models';

@Component({
  selector: 'app-us-map',
  templateUrl: './us-map.component.svg',
  styleUrls: ['./us-map.component.scss']
})
export class UsMapComponent {
  private currentGameLkp: Set<string> | null;
  private pastGamesLkp: Map<string, number>;
  private totalPastGames: number;

  @Input()
  public set currentGame(val: LicensePlate[] | null)
  {
    if (!val) {
      return;
    }

    this.currentGameLkp = new Set<string>();
    val.forEach(element => {
      if (element.country !== 'US') {
        return;
      }
      this.currentGameLkp?.add(element.stateOrProvince);
    });
  }

  @Input()
  public set pastGames(val: LicensePlate[][]) {
    this.pastGamesLkp = new Map<string, number>();
    this.totalPastGames = val.filter(v => v.length > 0).length;
    val.forEach(game => {
      game.forEach(element => {
        if (element.country !== 'US') {
          return;
        }
        let spots = this.pastGamesLkp.get(element.stateOrProvince);
        spots = spots === undefined ? 1: spots+1;
        this.pastGamesLkp.set(element.stateOrProvince, spots);
      });
    });
  }

  constructor(private readonly elementRef: ElementRef) {
    this.currentGameLkp = null;
    this.pastGamesLkp = new Map<string, number>();
    this.totalPastGames = 0;
  }

  public getWeightClass(state: string) {
    if (this.currentGameLkp?.has(state)) {
      return ['this-game-spot'];
    }

    const numOfSpots = this.pastGamesLkp.get(state);
    if (numOfSpots === undefined) {
      return [];
    }

    if (!!this.currentGameLkp) {
      return ['past-games-unweighted'];
    }

    const weight = numOfSpots / Math.max(1, this.totalPastGames);
    return [`past-games-weight-${Math.ceil(weight * 100 / 10) * 10}`];
  }
}
