import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { ServiceWorkerModule } from '@angular/service-worker';
import { environment } from '../environments/environment';

import { GeneralModule } from './general/general.module';
import { CoreModule } from './core/core.module'
import { appInitFactory } from './core/appInitFactory';
import { AppInitDataService } from './core/services/app-init-data.service';
import { AppComponent } from './app.component';

import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list'


@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    CoreModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    ServiceWorkerModule.register('ngsw-worker.js', { enabled: environment.production }),
    BrowserAnimationsModule,
    GeneralModule
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
