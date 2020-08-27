import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { UsMapComponent } from './us-map/us-map.component';

@NgModule({
  declarations: [UsMapComponent],
  imports: [
    CommonModule,
    MatCardModule,
    MatDividerModule,
    MatButtonModule,
    MatIconModule
  ],
  exports: [UsMapComponent, MatCardModule, MatDividerModule, MatButtonModule, MatIconModule]
})
export class GeneralModule { }
