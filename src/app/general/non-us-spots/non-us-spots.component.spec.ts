import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NonUsSpotsComponent } from './non-us-spots.component';

describe('NonUsSpotsComponent', () => {
  let component: NonUsSpotsComponent;
  let fixture: ComponentFixture<NonUsSpotsComponent>;

  beforeEach(async(() => {
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
