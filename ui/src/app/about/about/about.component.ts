import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.scss']
})
export class AboutComponent implements OnInit {

  constructor(private readonly gameSvc: GameService) { }

  ngOnInit(): void {
  }

  public resetAll(): void {
    this.gameSvc.resetAll();
  }
}
