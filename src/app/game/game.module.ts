import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';

import { GameRoutingModule } from './game-routing.module';
import { GameComponent } from './game/game.component';
import { ReactiveFormsModule } from '@angular/forms';
import { GeneralModule } from '../general/general.module';
import { SpotDialogComponent } from './spot-dialog/spot-dialog.component';
import { ConfirmationDialogComponent } from './confirmation-dialog/confirmation-dialog.component';


@NgModule({
  declarations: [GameComponent, SpotDialogComponent, ConfirmationDialogComponent],
  imports: [
    CommonModule,
    GameRoutingModule,
    ReactiveFormsModule,
    GeneralModule,
    MatCheckboxModule,
    MatDialogModule
  ],
  entryComponents: [SpotDialogComponent, ConfirmationDialogComponent]
})
export class GameModule { }
