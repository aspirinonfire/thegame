import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { NonUsSpotsComponent } from './non-us-spots.component';

describe('NonUsSpotsComponent', () => {
  let component: NonUsSpotsComponent;
  let fixture: ComponentFixture<NonUsSpotsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ NonUsSpotsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NonUsSpotsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
