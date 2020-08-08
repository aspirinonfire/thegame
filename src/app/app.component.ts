import { Component } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { AppRoutes } from './core/constants';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  private readonly _isLoading$: Subject<boolean>;

  constructor() {
    this._isLoading$ = new Subject<boolean>();
    this._isLoading$.next(false);
  }

  public get isLoading$(): Observable<boolean> {
    return this._isLoading$.asObservable();
  }

  public get appRoutes() {
    return AppRoutes;
  }
}
