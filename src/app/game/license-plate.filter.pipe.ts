import { Pipe, PipeTransform } from '@angular/core';
import { plateVm } from './models';

@Pipe({
  name: 'licensePlateFilter'
})
export class LicensePlateFilterPipe implements PipeTransform {

  transform(values: plateVm[], searchValue: string): plateVm[] {
    if (!searchValue) {
      return values;
    }

    var searchTerm = searchValue.toLowerCase();

    return values.filter((plate: any) =>
    {
      var plateName = plate.name.toLowerCase();

      // full name starts with
      if (plateName.startsWith(searchTerm)) {
        return true;
      }

      // contained in the second word
      if (plateName.includes(` ${searchTerm}`)) {
        return true;
      }

      // short name matches
      if (plate.stateOrProvince.toLowerCase() == searchTerm) {
        return true;
      } 

      return false;
    })
  }
}
