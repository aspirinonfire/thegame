import { Component, OnInit, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { plateVm, SpotDialogData } from '../models';
import { FormGroup, FormBuilder } from '@angular/forms';
import { Subscription } from 'rxjs';
import { GameService } from 'src/app/core/services/game.service';

@Component({
  selector: 'app-spot-dialog',
  templateUrl: './spot-dialog.component.html',
  styleUrls: ['./spot-dialog.component.scss']
})
export class SpotDialogComponent implements OnInit, OnDestroy {
  private readonly _subs: Subscription[];
  private readonly _clonedStates: plateVm[];
  private readonly _licenseGroup: FormGroup;
  public readonly licenseGroupName: string = "license";
  public readonly searchCtrlName: string = "search";
  form: FormGroup;

  constructor(private readonly dialogRef: MatDialogRef<SpotDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public plateData: SpotDialogData,
    private readonly fb: FormBuilder) { 
      this._clonedStates = plateData.plates.map((d: plateVm) => {
        return <plateVm>{ ...d };
      });

      this.form = this.fb.group({});
      this._licenseGroup = this.fb.group({});
      this.form.registerControl(this.licenseGroupName, this._licenseGroup);
      this._subs = [];
  }

  ngOnInit(): void {
    this.form.registerControl(this.searchCtrlName, this.fb.control(null));
    for (const state of this._clonedStates) {
      const ctrl = this.fb.control(null);
      this._licenseGroup.registerControl(state.key, ctrl);
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
    const formValues = <{ [K: string]: boolean }>this._licenseGroup.getRawValue();
    const newVals = this._clonedStates
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
    const ctrl = this._licenseGroup.get(state);
    if (!ctrl) {
      return;
    }
    ctrl.setValue(!ctrl.value);
    this.form.get(this.searchCtrlName)?.reset();
  }

  public dismiss() {
    this.dialogRef.close(null);
  }

  public trackPlateSpot(index: number, state: plateVm | null) {
    return state?.key;
  }

  public getPlateScorePoints(plate: plateVm) : number {
    return GameService.ScopeMultiplierByPlateLkp.get(`${plate.country}-${plate.stateOrProvince}`) ?? 1;
  }

  get searchValue(): string {
    return this.form.get(this.searchCtrlName)?.value;
  }
}
