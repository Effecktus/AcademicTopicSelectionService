import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { SharedModule } from 'primeng/api';

import type { UserListItemDto } from '../../../core/models/admin.models';

@Component({
  selector: 'app-user-detail-dialog',
  imports: [Button, Dialog, DatePipe, SharedModule],
  templateUrl: './user-detail-dialog.component.html',
  styleUrl: './user-detail-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailDialogComponent {
  readonly visible = model(false);
  readonly user = input.required<UserListItemDto>();
}
