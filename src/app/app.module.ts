import { BrowserModule } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CoreModule } from './core/core.module'

import { AppComponent } from './app.component';
import { RouterModule } from '@angular/router';
import { AppInitService } from './core/services/app-init.service';
import { of } from 'rxjs';
import { delay, map } from 'rxjs/operators';

// TODO export to core
export function appInitFactory(appInitService: AppInitService) {
  return (): Promise<void> => {
    // TODO run real data init call
    return of("some_data")
      .pipe(delay(2000))
      .pipe(map(x => {
        return appInitService.loadInitData(x);
      }))
      .toPromise();
  }
}

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    CoreModule,
    RouterModule
  ],
  providers: [
    AppInitService,
    {
      provide: APP_INITIALIZER,
      deps: [AppInitService],
      useFactory: appInitFactory,
      multi: true
    }],
  bootstrap: [AppComponent]
})
export class AppModule { }
