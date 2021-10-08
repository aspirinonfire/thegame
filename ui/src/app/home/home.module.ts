import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';

import { HomeRoutingModule } from './home-routing.module';
import { HomeComponent } from './home/home.component';
import { GeneralModule } from '../general/general.module';
import { HistoryComponent } from './history/history.component';


@NgModule({
  declarations: [HomeComponent, HistoryComponent],
  imports: [
    CommonModule,
    HomeRoutingModule,
    GeneralModule,
    MatListModule
  ]
})
export class HomeModule { }
