import { BrowserModule } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CoreModule } from './core/core.module'

import { AppComponent } from './app.component';
import { RouterModule } from '@angular/router';
import { AppInitService } from './core/services/app-init.service';
import { appInitFactory } from './core/appInitFactory';
import { GameService } from './core/services/game.service';

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
