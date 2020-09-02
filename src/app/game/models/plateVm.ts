import { LicensePlate } from 'src/app/core/models';

export interface plateVm extends LicensePlate {
  key: string,
  name: string,
  showDetails: boolean,
}