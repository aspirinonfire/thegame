import { Component, OnInit, OnDestroy } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { Game, UsStates, CanadaProvinces, LicensePlate } from 'src/app/core/models';
import { FormBuilder, FormGroup, FormControl, AbstractControl } from '@angular/forms';
import { distinctUntilChanged } from 'rxjs/operators';
import { Subscription } from 'rxjs';

export interface plateVm {
  key: UsStates | CanadaProvinces,
  name: string,
  showDetails: boolean,
  spottedBy: string | null,
  spottedOn: Date | null
}

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.scss']
})
export class GameComponent implements OnInit, OnDestroy {
  private readonly _subs: Subscription[];

  game: Game | null;
  form: FormGroup;
  allstates: plateVm[];

  constructor(private readonly gameSvc: GameService, private readonly fb: FormBuilder) {
    this.game = null;
    this.form = this.fb.group({});
    this.allstates = [];
    this._subs = [];
  }

  ngOnInit(): void {
    this.game = this.gameSvc.getCurrentGame();

    // TODO add Canada
    // TODO make it app singleton
    this.allstates = Object.keys(UsStates)
      .map(key => {
        let spottedBy = null;
        let spottedOn = null;
        let showDetails = false;
        if (this.game) {
          const sighting = this.game.licensePlates[key];
          spottedBy = sighting?.spottedBy;
          spottedOn = sighting?.dateSpotted;
          showDetails = !!sighting;
        }

        return <plateVm>{
          key: key,
          name: (<any>UsStates)[key],
          showDetails: showDetails,
          spottedBy: spottedBy,
          spottedOn: spottedOn
        }
      });

    for (const state of this.allstates) {
      const ctrl = this.fb.control(null);
      const sub = ctrl.valueChanges
        .pipe(distinctUntilChanged())
        .subscribe(val => {
          console.log(state.key, val);
          if (val !== null) {
            const spottedBy = "Alex";
            this.gameSvc.saveSpottedPlate(state.key, spottedBy);
            if (val) {
              state.showDetails = true;
              state.spottedBy = spottedBy;
              state.spottedOn = new Date();
            } else {
              state.showDetails = false;
              state.spottedBy = null;
              state.spottedOn = null;
            }
          }
        });
      this._subs.push(sub);
      this.form.registerControl(state.key, ctrl);
    }
    this.form.reset();
    this.loadFormValues();
  }

  ngOnDestroy() {
    this._subs.forEach(sub => sub.unsubscribe());
  }

  public startNewGame(): void {
    this.game = this.gameSvc.createGame("Test game", "Alex");
    this.loadFormValues();
    this.form.reset();
  }

  private loadFormValues() {
    if (!this.game || !this.form) {
      return;
    }

    Object.keys(this.game.licensePlates)
      .forEach(k => {
        const key =  <UsStates | CanadaProvinces>k;
        const ctrl = this.form.get(key);
        if (!ctrl) {
          return;
        }
        ctrl.setValue(true, { onlySelf: true, emitEvent: false });
      });
  }
}
