import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { CoreRoutingModule } from './core-routing.module';
import { RouterModule } from '@angular/router';


@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    CoreRoutingModule,
    NgbModule,
    RouterModule
  ],
  exports: [
    NgbModule,
    RouterModule
  ]
})
export class CoreModule { }
