import { Component, OnInit } from '@angular/core';
import { GameVm, LicensePlate } from 'src/app/core/models';
import { GameService } from 'src/app/core/services/game.service';
import { Router } from '@angular/router';
import { AppRoutes } from 'src/app/core/constants';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  public currentGame: GameVm | null;
  public pastGames: GameVm[];
  public error: string | null;

  constructor(private readonly gameSvc: GameService,
    private readonly router: Router) {
    
    this.currentGame = null;
    this.pastGames = [];
    this.error = null;
  }

  ngOnInit(): void {
    const currGame = this.gameSvc.getCurrentGame();
    if (!!currGame) {
      this.currentGame = new GameVm(currGame);
    }
    this.pastGames = this.gameSvc.getPastGames()
      .map(g => new GameVm(g));
  }

  public get hasPastGames(): boolean {
    return !!this.pastGames.length;
  }

  public get numOfGames(): number {
    if (!this.hasPastGames) {
      return 0;
    }

    return this.pastGames.length;
  }

  public get lastSpot(): LicensePlate | null {
    const game = this.gameSvc.getCurrentGame();
    if (!game) {
      return null;
    }

    return Object.keys(game.licensePlates)
      .map(key => game.licensePlates[key])
      .sort((a, b) => {
        const aDate = a.dateSpotted || 0;
        const bDate = b.dateSpotted || 0;
        return aDate < bDate ? 1 : -1;
      })[0];
  }

  public get spottedUsStates(): ReadonlyMap<string, number>{
    if (!this.hasPastGames) {
      return new Map<string, number>();
    }

    const spottedUsStates = this.pastGames
      .map(pg => pg.licensePlates)
      .reduce((sum, plate) => {
        Object.values(plate)
          .filter(val => val.country === 'US')
          .forEach(val => {
            let counter = sum.get(val.stateOrProvince);
            if (!counter) {
              counter = 0;
            }
            counter++;
            sum.set(val.stateOrProvince, counter);
          });

        return sum;
      }, new Map<string, number>());
    return spottedUsStates;
  }

  openCurrentGame() {
    this.router.navigate(['..', AppRoutes.game]);
  }
}
