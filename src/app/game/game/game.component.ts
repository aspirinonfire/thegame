import { Component, OnInit, OnDestroy } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { Game, Country, LicensePlate } from 'src/app/core/models';
import { FormBuilder, FormGroup, FormControl, AbstractControl } from '@angular/forms';
import { distinctUntilChanged } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { AppInitDataService } from 'src/app/core/services/app-init-data.service';

export interface plateVm extends LicensePlate {
  key: string,
  name: string,
  showDetails: boolean,
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

  constructor(private readonly gameSvc: GameService,
    private readonly initData: AppInitDataService,
    private readonly fb: FormBuilder) {
    
    this.game = null;
    this.form = this.fb.group({});
    this.allstates = [];
    this._subs = [];
  }

  ngOnInit(): void {
    this.game = this.gameSvc.getCurrentGame();

    this.setVm();

    for (const state of this.allstates) {
      const ctrl = this.fb.control(null);
      const sub = ctrl.valueChanges
        .pipe(distinctUntilChanged())
        .subscribe(val => {
          if (val !== null) {
            const spottedBy = this.initData.account.name;
            this.gameSvc.saveSpottedPlate(state.stateOrProvince, state.country, spottedBy);
            if (val) {
              state.showDetails = true;
              state.spottedBy = spottedBy;
              state.dateSpotted = new Date();
            } else {
              state.showDetails = false;
              state.spottedBy = null;
              state.dateSpotted = null;
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

  public get currentGame(): LicensePlate[] {
    const game = this.gameSvc.getCurrentGame();
    if (!game) {
      return [];
    }

    return Object.keys(game.licensePlates).map(key => game.licensePlates[key]);
  }

  public get pastGames(): LicensePlate[][] {
    const pastGames = this.gameSvc.getPastGames();
    if (!pastGames) {
      return []
    }

    return pastGames.map(game => {
      return Object.keys(game.licensePlates).map(key => game.licensePlates[key]);
    });
  }

  public startNewGame(): void {
    this.game = this.gameSvc.createGame("Test game", this.initData.account.name);
    this.setVm();
    this.loadFormValues();
    this.form.reset();
  }

  private setVm() {
    this.allstates = [...this.initData.gameData.values()]
      .map(ter => {
        const key = `${ter.country}-${ter.shortName}`;
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
          name: ter.country != 'US' ? `${ter.longName} (${ter.country})` : ter.longName,
          stateOrProvince: ter.shortName,
          dateSpotted: spottedOn,
          country: ter.country,
          showDetails: showDetails,
          spottedBy: spottedBy
        }
      });
  }

  private loadFormValues() {
    if (!this.game || !this.form) {
      return;
    }

    Object.keys(this.game.licensePlates)
      .forEach(stateOrProvince => {
        const plate = this.game?.licensePlates[stateOrProvince];
        const key = `${plate?.country}-${plate?.stateOrProvince}`;
        const ctrl = this.form.get(key);
        if (!ctrl) {
          return;
        }
        ctrl.setValue(true, { onlySelf: true, emitEvent: false });
      });
  }
}
