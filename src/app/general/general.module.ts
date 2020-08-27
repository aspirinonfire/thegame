import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsMapComponent } from './us-map/us-map.component';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatButtonModule } from '@angular/material/button';

@NgModule({
  declarations: [UsMapComponent],
  imports: [
    CommonModule,
    MatCardModule,
    MatDividerModule,
    MatButtonModule
  ],
  exports: [UsMapComponent, MatCardModule, MatDividerModule, MatButtonModule]
})
export class GeneralModule { }
