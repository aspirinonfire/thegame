import { Component, OnInit } from '@angular/core';
import { Game } from 'src/app/core/models';
import { GameService } from 'src/app/core/services/game.service';
import { Router, ActivatedRoute } from '@angular/router';
import { AppRoutes } from 'src/app/core/constants';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  public currentGame: Game | null;
  public pastGames: Game[];
  public error: string | null;

  constructor(private readonly gameSvc: GameService,
    private readonly router: Router) {
    
    this.currentGame = null;
    this.pastGames = [];
    this.error = null;
  }

  ngOnInit(): void {
    this.currentGame = this.gameSvc.getCurrentGame();
    this.pastGames = this.gameSvc.getPastGames();
  }

  public get hasActiveGame(): boolean {
    return !!this.currentGame;
  }

  public get hasPastGames(): boolean {
    return !!this.pastGames.length;
  }

  public resetAll(): void {
    this.gameSvc.resetAll();
    this.ngOnInit();
  }
}
