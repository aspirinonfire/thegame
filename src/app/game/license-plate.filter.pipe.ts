import { Pipe, PipeTransform } from '@angular/core';
import { plateVm } from './models';

@Pipe({
  name: 'licensePlateFilter'
})
export class LicensePlateFilterPipe implements PipeTransform {

  transform(values: plateVm[], searchValue: string): plateVm[] {
    if (!searchValue) return values;
    return values.filter((v: any) =>
      v.name.toLowerCase().indexOf(searchValue.toLowerCase()) > -1)
  }
}
