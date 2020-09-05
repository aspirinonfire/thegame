import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FlexLayoutModule } from '@angular/flex-layout';

import { UsMapComponent } from './us-map/us-map.component';

const reExports = [
  CommonModule,
  MatCardModule,
  MatDividerModule,
  MatButtonModule,
  MatIconModule,
  FlexLayoutModule
]

@NgModule({
  declarations: [UsMapComponent],
  imports: reExports,
  exports: [UsMapComponent, ...reExports]
})
export class GeneralModule { }
