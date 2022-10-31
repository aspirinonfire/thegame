import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { GameVm, LicensePlate } from 'src/app/core/models';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.scss']
})
export class HistoryComponent implements OnInit {
  public pastGames: GameVm[];

  constructor(private readonly gameSvc: GameService) { 
    this.pastGames = [];
  }

  ngOnInit(): void {
    this.pastGames = this.gameSvc.getPastGames()
      .sort((a, b) => {
        if (!a.dateFinished || !b.dateFinished) {
          return -1;
        }
        return a.dateFinished > b.dateFinished ? -1 : 1;
      })
      .map(g => new GameVm(g));
  }

  public get hasPastGames(): boolean {
    return !!this.pastGames.length;
  }

  public get pastGameLicensePlates(): LicensePlate[][] {
    if (!this.pastGames) {
      return []
    }

    return this.pastGames.map(game => {
      return Object.keys(game.licensePlates).map(key => game.licensePlates[key]);
    });
  }

  public get spottedUsStates(): ReadonlyMap<string, number> {
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

  public GetPastGameScore(pastGame: GameVm): number {
    if (pastGame.score != null) {
      return pastGame.score.totalScore;
    }
    return pastGame.platesSpotted;
  }
}
