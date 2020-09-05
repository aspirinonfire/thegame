import { Component, OnInit, AfterViewInit, ElementRef, OnDestroy, Input } from '@angular/core';
import { Subscription, Subject, interval } from 'rxjs';
import { debounce } from 'rxjs/operators';
import { LicensePlate } from 'src/app/core/models';

@Component({
  selector: 'app-us-map',
  templateUrl: './us-map.component.svg',
  styleUrls: ['./us-map.component.scss']
})
export class UsMapComponent implements OnInit, AfterViewInit, OnDestroy {
  private subs: Subscription[];
  private resizeTrigger$: Subject<boolean>;
  private currentGameLkp: Set<string> | null;
  private pastGamesLkp: Map<string, number>;
  private totalPastGames: number;

  @Input()
  public set currentGame(val: LicensePlate[] | null)
  {
    if (!val) {
      return;
    }

    this.currentGameLkp = new Set<string>();
    val.forEach(element => {
      if (element.country !== 'US') {
        return;
      }
      this.currentGameLkp?.add(element.stateOrProvince);
    });
  }

  @Input()
  public set pastGames(val: LicensePlate[][]) {
    this.pastGamesLkp = new Map<string, number>();
    this.totalPastGames = val.filter(v => v.length > 0).length;
    val.forEach(game => {
      game.forEach(element => {
        if (element.country !== 'US') {
          return;
        }
        let spots = this.pastGamesLkp.get(element.stateOrProvince);
        spots = spots === undefined ? 1: spots+1;
        this.pastGamesLkp.set(element.stateOrProvince, spots);
      });
    });
  }

  constructor(private readonly elementRef: ElementRef) {
    this.subs = [];
    this.resizeTrigger$ = new Subject<boolean>();
    this.currentGameLkp = null;
    this.pastGamesLkp = new Map<string, number>();
    this.totalPastGames = 0;
  }

  ngOnInit(): void {
    const sub = this.resizeTrigger$
      .pipe(debounce(() => interval(100)))
      .subscribe(e => {
        this.redrawMap();
      });
    this.subs.push(sub);
  }

  ngAfterViewInit(): void {
    this.redrawMap();
  }

  ngOnDestroy(): void {
    for (const sub of this.subs) {
      sub.unsubscribe();
    }
    this.resizeTrigger$.complete();
  }

  public getWeightClass(state: string) {
    if (this.currentGameLkp?.has(state)) {
      return ['this-game-spot'];
    }

    const numOfSpots = this.pastGamesLkp.get(state);
    if (numOfSpots === undefined) {
      return [];
    }

    if (!!this.currentGameLkp) {
      return ['past-games-unweighted'];
    }

    const weight = numOfSpots / Math.max(1, this.totalPastGames);
    return [`past-games-weight-${Math.ceil(weight * 100 / 10) * 10}`];
  }

  private redrawMap() {
    console.log('redrawing');
    const element = this.elementRef.nativeElement;
    const svg = element.querySelector('svg');
    const parentContainerWidth = element.parentElement.clientWidth;

    const viewBoxWidth = 1000;
    const viewBoxHeight = 800;
    const heightOffest = window.innerWidth < viewBoxWidth ?
      Math.max(40, window.innerWidth / viewBoxWidth * 85) : 85;
    
    svg.setAttribute('width', `${parentContainerWidth}px`);
    svg.setAttribute('height', `${heightOffest}vh`);
    svg.setAttribute('viewBox', `0 0 ${viewBoxWidth} ${viewBoxHeight}`);
    svg.style.display = 'block';
  }

  onResize(event: any) {
    this.resizeTrigger$.next(true);
  }
}
