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
  public error: string | null;

  constructor(private readonly gameSvc: GameService,
    private readonly router: Router) {
    
    this.currentGame = null;
    this.error = null;
  }

  ngOnInit(): void {
    const currGame = this.gameSvc.getCurrentGame();
    if (!!currGame) {
      this.currentGame = new GameVm(currGame);
    }
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

  openCurrentGame() {
    this.router.navigate(['..', AppRoutes.game]);
  }
}
