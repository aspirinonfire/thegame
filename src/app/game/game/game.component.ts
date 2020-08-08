import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { Game, UsStates, CanadaProvinces, LicensePlate } from 'src/app/core/models';
import { FormBuilder, FormGroup, FormControl, AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.scss']
})
export class GameComponent implements OnInit {
  game: Game | null;
  form: FormGroup;
  allstates: { key: string; name: string; }[];

  constructor(private readonly gameSvc: GameService, private readonly fb: FormBuilder) {
    this.game = null;
    this.form = this.fb.group({});
    this.allstates = [];
  }

  ngOnInit(): void {
    this.game = this.gameSvc.getCurrentGame();

    // TODO add Canada
    this.allstates = Object.keys(UsStates)
      .map(key => <{key: string, name: string}>{
        key: key,
        name: (<any>UsStates)[key]
      });
    this.resetForm();

    // TODO subscribe to control changes
  }

  public startNewGame(): void {
    this.game = this.gameSvc.createGame("Test game", "Alex");
    this.resetForm();
  }

  private resetForm() {
    for (const state of this.allstates) {
      const currentVal = !!this.game ? !!this.game.licensePlates[state.key] : false;
      this.form.addControl(state.key, this.fb.control(currentVal));
    }
    this.form.reset();
  }
}
