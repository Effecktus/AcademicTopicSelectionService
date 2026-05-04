import { TestBed } from '@angular/core/testing';

import { TopicsPageComponent } from './topics-page.component';

describe('TopicsPageComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TopicsPageComponent],
    });
  });

  it('создаётся без ошибок', () => {
    const fixture = TestBed.createComponent(TopicsPageComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });
});
