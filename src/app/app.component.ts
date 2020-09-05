import { Component, OnInit } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { AppRoutes } from './core/constants';
import { SwUpdate } from '@angular/service-worker';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  private readonly _isLoading$: Subject<boolean>;
  public isMenuCollapsed: boolean;

  constructor(private readonly updateSvc: SwUpdate) {
    this._isLoading$ = new Subject<boolean>();
    this._isLoading$.next(false);
    this.isMenuCollapsed = true;
  }

  ngOnInit() {
    this.updateSvc.available
      .subscribe(event =>
        this.updateSvc.activateUpdate()
          .then(() => document.location.reload()));
  }

  public get isLoading$(): Observable<boolean> {
    return this._isLoading$.asObservable();
  }

  public forceCollapse() {
    if (!this.isMenuCollapsed) {
      this.isMenuCollapsed = true;
    }
  }

  public get appRoutes() {
    return AppRoutes;
  }
}
