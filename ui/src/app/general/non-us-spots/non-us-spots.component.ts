import { Component, OnInit, Input } from '@angular/core';
import { LicensePlate } from 'src/app/core/models';
import { NonUsSpot } from '../models';
import { GameService } from 'src/app/core/services/game.service';

@Component({
  selector: 'app-non-us-spots',
  templateUrl: './non-us-spots.component.html',
  styleUrls: ['./non-us-spots.component.scss']
})
export class NonUsSpotsComponent implements OnInit {

  private currentGamePlates: string[] | null;
  private pastGamesLkp: Map<string, number>;

  @Input()
  public set currentGame(val: LicensePlate[] | null) {
    if (!val) {
      return;
    }

    this.currentGamePlates = [];
    val.forEach(element => {
      if (element.country === 'US') {
        return;
      }
      this.currentGamePlates?.push(element.stateOrProvince);
    });
  }

  @Input()
  public set pastGames(val: LicensePlate[][]) {
    this.pastGamesLkp = new Map<string, number>();
    val.forEach(game => {
      game.forEach(element => {
        if (element.country === 'US') {
          return;
        }
        let spots = this.pastGamesLkp.get(element.stateOrProvince);
        spots = spots === undefined ? 1 : spots + 1;
        this.pastGamesLkp.set(element.stateOrProvince, spots);
      });
    });
  }

  public get nonUsPlates(): NonUsSpot[] {
    const pastGames = [...this.pastGamesLkp.entries()]
      .map((kvp: [string, number]) => <NonUsSpot>{
        province: kvp[0],
        previousSpots: kvp[1],
        isInCurrentGame: false
      })
      .sort((a, b) => a.previousSpots - b.previousSpots);

    const currGame = (this.currentGamePlates || [])
      .map(p => <NonUsSpot> {
        province: p,
        previousSpots: 0,
        isInCurrentGame: true
      })
      .sort((a,b) => {
        return a.province.toUpperCase() > b.province.toUpperCase() ? 1 : -1;
      });

    const allPlates = currGame.concat(pastGames).map(p => {
      p.fullName = this.gameSvc.getLongName(p.province, 'CA');
      return p;
    });
    const map = new Map<string, NonUsSpot>();
    for (const plate of allPlates) {
      if (plate.isInCurrentGame) {
        map.set(plate.province, plate);
        continue;
      }
      if (map.has(plate.province)) {
        continue;
      }
      map.set(plate.province, plate);
    }

    return [...map.values()];
  }

  public get hasNonUsPlates(): boolean {
    return !!this.nonUsPlates.length;
  }

  constructor(private readonly gameSvc: GameService) {
    this.currentGamePlates = null;
    this.pastGamesLkp = new Map<string, number>();
  }

  ngOnInit(): void {

  }

  public plateTrackBy(index: number, plate: NonUsSpot | null) {
    return plate?.province;
  }
}
