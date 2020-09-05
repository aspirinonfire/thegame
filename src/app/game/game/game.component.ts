import { Component, OnInit, OnDestroy } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { Game, LicensePlate } from 'src/app/core/models';
import { Subscription } from 'rxjs';
import { AppInitDataService } from 'src/app/core/services/app-init-data.service';
import { Router } from '@angular/router';
import { AppRoutes } from 'src/app/core/constants';
import { MatDialog } from '@angular/material/dialog';
import { SpotDialogComponent } from '../spot-dialog/spot-dialog.component';
import { plateVm, SpotDialogData } from '../models';

@Component({
  selector: 'app-game',
  templateUrl: './game.component.html',
  styleUrls: ['./game.component.scss']
})
export class GameComponent implements OnInit, OnDestroy {
  private readonly _subs: Subscription[];
  allstates: plateVm[];

  constructor(private readonly gameSvc: GameService,
    private readonly initData: AppInitDataService,
    private readonly router: Router,
    private dialog: MatDialog) {
    
    this.allstates = [];
    this._subs = [];
  }

  ngOnInit(): void {
    this.setVm();
  }

  ngOnDestroy() {
    this._subs.forEach(sub => sub.unsubscribe());
  }

  public get currentGameSpots(): LicensePlate[] {
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

  public openAddDialog() {
    if (!!this.currentGame) {
      this.openSpotDialog();
    } else {
      this.openNewGameDialog();
    }
  }

  public finishGame(): void {
    const res = this.gameSvc.finishActiveGame();
    if (typeof res === 'string') {
      // TODO show error!
      return;
    }
    this.router.navigate(['..', AppRoutes.home]);
    return;
  }

  public get currentGame() : Game | null {
    return this.gameSvc.getCurrentGame();
  }

  private openSpotDialog() {
    const dialogRef = this.dialog.open(SpotDialogComponent, {
      data: <SpotDialogData>{
        name: this.initData.account.name,
        plates: this.allstates
      }
    });

    dialogRef.afterClosed().subscribe((result: plateVm[]) => {
      if (!result) {
        return;
      }
      result.forEach(res => this.gameSvc.saveSpottedPlate(res.stateOrProvince, res.country, this.initData.account.name));
      this.setVm();
    });
  }

  private openNewGameDialog() {
    // TODO implement dialog
    const newGame = this.gameSvc.createGame("Test game", this.initData.account.name);
    if (typeof newGame === 'string') {
      // TODO show error!
      return;
    }
    this.setVm();
  }

  private setVm() {
    const lkp = new Map<string, LicensePlate>();
    this.currentGameSpots.forEach(p => lkp.set(p.stateOrProvince, p));

    this.allstates = [...this.initData.gameData.values()]
      .map(ter => {
        const key = `${ter.country}-${ter.shortName}`;
        let spottedBy = null;
        let spottedOn = null;
        let showDetails = false;

        const sighting = lkp.get(ter.shortName);
        spottedBy = sighting?.spottedBy;
        spottedOn = sighting?.dateSpotted;
        showDetails = !!sighting;

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
}
