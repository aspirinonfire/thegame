import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.scss']
})
export class GameComponent implements OnInit {

  constructor(private readonly gameSvc: GameService) { }

  ngOnInit(): void {
  }

  public startNewGame(): void {
    this.gameSvc.createGame("Test game", "Alex");
  }
}
