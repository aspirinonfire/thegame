import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';

import { UsMapComponent } from './us-map/us-map.component';
import { NonUsSpotsComponent } from './non-us-spots/non-us-spots.component';

const reExports = [
  CommonModule,
  MatCardModule,
  MatDividerModule,
  MatButtonModule,
  MatIconModule,
  FlexLayoutModule,
  MatProgressSpinnerModule,
  MatChipsModule
];

const generalComponents = [
  UsMapComponent,
  NonUsSpotsComponent
];

@NgModule({
  declarations: generalComponents,
  imports: reExports,
  exports: [...generalComponents, ...reExports]
})
export class GeneralModule { }
