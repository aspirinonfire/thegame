import { TestBed } from '@angular/core/testing';

import { AppInitDataService } from './app-init-data.service';

describe('AppInitService', () => {
  let service: AppInitDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AppInitDataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
