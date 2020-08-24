import { BrowserModule } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CoreModule } from './core/core.module'

import { AppComponent } from './app.component';
import { RouterModule } from '@angular/router';
import { AppInitDataService } from './core/services/app-init-data.service';
import { appInitFactory } from './core/appInitFactory';
import { ServiceWorkerModule } from '@angular/service-worker';
import { environment } from '../environments/environment';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatToolbarModule } from '@angular/material/toolbar';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    CoreModule,
    BrowserAnimationsModule,
    MatToolbarModule,
    ServiceWorkerModule.register('ngsw-worker.js', { enabled: environment.production }),
    BrowserAnimationsModule,
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
