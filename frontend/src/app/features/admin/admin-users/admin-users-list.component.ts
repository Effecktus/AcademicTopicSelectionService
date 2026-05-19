import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';

import type { DepartmentDto, UserListItemDto, UserRoleDto } from '../../../core/models/admin.models';
import { AdminApiService } from '../admin-api.service';
import { CreateUserDialogComponent } from './create-user-dialog.component';
import { UserDetailDialogComponent } from './user-detail-dialog.component';

interface RoleOption {
  label: string;
  value: string | null;
}

@Component({
  selector: 'app-admin-users-list',
  imports: [ReactiveFormsModule, Button, InputText, Select, DatePipe, CreateUserDialogComponent, UserDetailDialogComponent],
  templateUrl: './admin-users-list.component.html',
  styleUrl: './admin-users-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersListComponent {
  private readonly adminApi = inject(AdminApiService);

  readonly items = signal<UserListItemDto[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = 20;
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  readonly canGoPrev = computed(() => this.page() > 1);
  readonly canGoNext = computed(() => this.page() < this.totalPages());

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly roleControl = new FormControl<string | null>(null);

  readonly roles = signal<UserRoleDto[]>([]);
  readonly departments = signal<DepartmentDto[]>([]);

  readonly roleOptions = computed<RoleOption[]>(() => [
    { label: 'Все роли', value: null },
    ...this.roles().map((r) => ({ label: r.displayName, value: r.id })),
  ]);

  readonly showCreateDialog = signal(false);
  readonly selectedUser = signal<UserListItemDto | null>(null);
  readonly showDetailDialog = signal(false);

  openDetail(user: UserListItemDto): void {
    this.selectedUser.set(user);
    this.showDetailDialog.set(true);
  }

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.roleControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.page.set(1);
      this.load();
    });

    this.adminApi.getUserRoles().subscribe((roles) => this.roles.set(roles));
    this.adminApi.getDepartments().subscribe((depts) => this.departments.set(depts));
    this.load();
  }

  prevPage(): void {
    if (!this.canGoPrev()) return;
    this.page.update((v) => v - 1);
    this.load();
  }

  nextPage(): void {
    if (!this.canGoNext()) return;
    this.page.update((v) => v + 1);
    this.load();
  }

  openCreateDialog(): void {
    this.showCreateDialog.set(true);
  }

  onUserCreated(): void {
    this.showCreateDialog.set(false);
    this.page.set(1);
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi
      .getUsers({
        page: this.page(),
        pageSize: this.pageSize,
        roleId: this.roleControl.value ?? null,
        query: this.searchControl.value.trim() || null,
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.total.set(Number(result.total));
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Не удалось загрузить список пользователей.');
          this.isLoading.set(false);
        },
      });
  }
}
