import { BrowserModule } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CoreModule } from './core/core.module'

import { AppComponent } from './app.component';
import { RouterModule } from '@angular/router';
import { AppInitDataService } from './core/services/app-init-data.service';
import { appInitFactory } from './core/appInitFactory';
import { GameService } from './core/services/game.service';
import { ServiceWorkerModule } from '@angular/service-worker';
import { environment } from '../environments/environment';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    CoreModule,
    ServiceWorkerModule.register('ngsw-worker.js', { enabled: environment.production }),
  ],
  providers: [
    AppInitDataService,
    {
      provide: APP_INITIALIZER,
      deps: [AppInitDataService],
      useFactory: appInitFactory,
      multi: true
    }],
  bootstrap: [AppComponent]
})
export class AppModule { }
