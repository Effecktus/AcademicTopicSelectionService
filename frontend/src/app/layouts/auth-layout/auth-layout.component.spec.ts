import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthLayoutComponent } from './auth-layout.component';

describe('AuthLayoutComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AuthLayoutComponent],
      providers: [provideRouter([])],
    });
  });

  it('создаётся без ошибок', () => {
    const fixture = TestBed.createComponent(AuthLayoutComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });
});
