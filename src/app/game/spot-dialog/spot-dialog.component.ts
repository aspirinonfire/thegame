import { Component, OnInit, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { plateVm, SpotDialogData } from '../models';
import { FormGroup, FormBuilder } from '@angular/forms';
import { distinctUntilChanged } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-spot-dialog',
  templateUrl: './spot-dialog.component.html',
  styleUrls: ['./spot-dialog.component.scss']
})
export class SpotDialogComponent implements OnInit, OnDestroy {
  private readonly _subs: Subscription[];
  private readonly clonedStates: plateVm[];
  form: FormGroup;

  constructor(private readonly dialogRef: MatDialogRef<SpotDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public plateData: SpotDialogData,
    private readonly fb: FormBuilder) { 
      this.clonedStates = plateData.plates.map((d: plateVm) => {
        return <plateVm>{ ...d };
      });

      this.form = this.fb.group({});
      this._subs = [];
  }

  ngOnInit(): void {

    for (const state of this.clonedStates) {
      const ctrl = this.fb.control(null);
      this.form.registerControl(state.key, ctrl);
      if (!!state.dateSpotted) {
        ctrl.setValue(true);
      }
    }
    this.form.markAsPristine();
  }

  public ngOnDestroy() {
    this._subs.forEach(s => s.unsubscribe());
  }

  public submit() {
    const formValues = <{[K: string]: boolean}>this.form.getRawValue();
    const newVals = this.clonedStates
      .map(s => {
        const spot = formValues[s.key];
        if (spot == null || spot == undefined) {
          return null;
        }

        if ((spot && !!s.spottedBy) || (!spot && !s.spottedBy)) {
          return null;
        }
        
        return s;
      })
      .filter(s => s != null);
    this.dialogRef.close(newVals);
  }

  public togglePlate(state: string) {
    const ctrl = this.form.controls[state];
    ctrl.setValue(!ctrl.value);
  }

  public dismiss() {
    this.dialogRef.close(null);
  }

  public trackPlateSpot(index: number, state: plateVm | null) {
    return state?.key;
  }
}
