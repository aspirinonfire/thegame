import { Component, OnInit } from '@angular/core';
import { GameVm } from 'src/app/core/models';
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

  openCurrentGame() {
    this.router.navigate(['..', AppRoutes.game]);
  }
}
