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
import { LicensePlateFilterPipe } from './license-plate.filter.pipe';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from "@angular/material/input";

@NgModule({
    declarations: [
        GameComponent,
        SpotDialogComponent,
        ConfirmationDialogComponent,
        LicensePlateFilterPipe
    ],
    imports: [
        CommonModule,
        GameRoutingModule,
        ReactiveFormsModule,
        GeneralModule,
        MatCheckboxModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule
    ]
})
export class GameModule { }
