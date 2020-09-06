import { TestBed } from '@angular/core/testing';

import { GameRedirectGuard } from './game-redirect.guard';

describe('GameRedirectGuard', () => {
  let guard: GameRedirectGuard;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    guard = TestBed.inject(GameRedirectGuard);
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });
});
