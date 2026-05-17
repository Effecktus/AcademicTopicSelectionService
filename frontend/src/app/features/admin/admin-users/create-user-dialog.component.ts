import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { MessageService } from 'primeng/api';

import type { DepartmentDto, UserRoleDto } from '../../../core/models/admin.models';
import { AdminApiService } from '../admin-api.service';

@Component({
  selector: 'app-create-user-dialog',
  imports: [ReactiveFormsModule, Button, Dialog, InputText, Password, Select],
  templateUrl: './create-user-dialog.component.html',
  styleUrl: './create-user-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateUserDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly adminApi = inject(AdminApiService);
  private readonly messageService = inject(MessageService);

  readonly visible = model(false);
  readonly roles = input.required<UserRoleDto[]>();
  readonly departments = input.required<DepartmentDto[]>();
  readonly userCreated = output<void>();

  readonly isSubmitting = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    middleName: [''],
    roleId: ['', Validators.required],
    departmentId: [null as string | null],
  });

  onHide(): void {
    this.form.reset();
    this.serverError.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.serverError.set(null);

    this.adminApi
      .createUser({
        email: v.email!,
        password: v.password!,
        firstName: v.firstName!,
        lastName: v.lastName!,
        middleName: v.middleName || null,
        roleId: v.roleId!,
        departmentId: v.departmentId || null,
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.messageService.add({
            severity: 'success',
            summary: 'Пользователь создан',
            detail: `${v.lastName} ${v.firstName} успешно добавлен.`,
          });
          this.visible.set(false);
          this.userCreated.emit();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          const detail = err?.error?.detail ?? 'Не удалось создать пользователя.';
          this.serverError.set(detail);
        },
      });
  }

  hasError(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }
}
