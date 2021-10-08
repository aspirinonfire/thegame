import { LicensePlateFilterPipe } from './license-plate.filter.pipe';

describe('LicensePlateFilterPipe', () => {
  it('create an instance', () => {
    const pipe = new LicensePlateFilterPipe();
    expect(pipe).toBeTruthy();
  });
});
