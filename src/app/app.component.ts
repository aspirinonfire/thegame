import { Component } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { AppRoutes } from './core/constants';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  private readonly _isLoading$: Subject<boolean>;
  public isMenuCollapsed: boolean;

  constructor() {
    this._isLoading$ = new Subject<boolean>();
    this._isLoading$.next(false);
    this.isMenuCollapsed = true;
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
