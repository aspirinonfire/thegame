import { Component, OnInit, AfterViewInit, ElementRef, OnDestroy, Input } from '@angular/core';
import { Subscription, Observable, Subject } from 'rxjs';
import { throttleTime } from 'rxjs/operators';

@Component({
  selector: 'app-us-map',
  templateUrl: './us-map.component.svg',
  styleUrls: ['./us-map.component.scss']
})
export class UsMapComponent implements OnInit, AfterViewInit, OnDestroy {
  private subs: Subscription[];
  private resizeTrigger$: Subject<boolean>;

  @Input()
  public spottedStatesLkp: ReadonlyMap<string, number>;

  constructor(private readonly elementRef: ElementRef) {
    this.subs = [];
    this.resizeTrigger$ = new Subject<boolean>();
    this.spottedStatesLkp = new Map<string, number>();
  }

  ngOnInit(): void {
    const sub = this.resizeTrigger$
      .pipe(throttleTime(200))
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
    const weight = this.spottedStatesLkp.get(state);
    if (!weight) {
      return [];
    }
    return [`weight-${Math.ceil(weight * 100 / 10) * 10}`];
  }

  private redrawMap() {
    console.log('redrawing');
    const element = this.elementRef.nativeElement;
    const svg = element.querySelector('svg');

    const viewBoxWidth = 1100;
    const viewBoxHeight = 600;
    const heightOffest = window.innerWidth < viewBoxWidth ?
      Math.max(40, window.innerWidth / viewBoxWidth * 100) : 100;
    
    svg.setAttribute('width', '100vw');
    svg.setAttribute('height', `${heightOffest}vh`);
    svg.setAttribute('viewBox', `0 0 ${viewBoxWidth} ${viewBoxHeight}`);
    svg.style.display = 'block';
  }

  onResize(event: any) {
    this.resizeTrigger$.next(true);
  }
}
