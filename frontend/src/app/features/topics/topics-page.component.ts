import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Card } from 'primeng/card';

@Component({
  selector: 'app-topics-page',
  imports: [Card],
  templateUrl: './topics-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsPageComponent {}
