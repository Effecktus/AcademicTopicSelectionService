import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

export const appRoutes: Routes = [
  {
    path: 'login',
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/auth/login/login.component').then((m) => m.LoginComponent),
      },
    ],
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'topics' },
      {
        path: 'teachers',
        loadComponent: () =>
          import('./features/teachers/teachers-list/teachers-list.component').then(
            (m) => m.TeachersListComponent,
          ),
      },
      {
        path: 'teachers/:id',
        loadComponent: () =>
          import('./features/teachers/teacher-detail/teacher-detail.component').then(
            (m) => m.TeacherDetailComponent,
          ),
      },
      {
        path: 'topics',
        loadComponent: () =>
          import('./features/topics/topics-list/topics-list.component').then(
            (m) => m.TopicsListComponent,
          ),
      },
      {
        path: 'topics/new',
        canActivate: [roleGuard],
        data: { role: 'Teacher' },
        loadComponent: () =>
          import('./features/topics/topic-form/topic-form.component').then((m) => m.TopicFormComponent),
      },
      {
        path: 'topics/:id',
        loadComponent: () =>
          import('./features/topics/topic-detail/topic-detail.component').then(
            (m) => m.TopicDetailComponent,
          ),
      },
      {
        path: 'topics/:id/edit',
        canActivate: [roleGuard],
        data: { role: 'Teacher' },
        loadComponent: () =>
          import('./features/topics/topic-form/topic-form.component').then((m) => m.TopicFormComponent),
      },
      {
        path: 'supervisor-requests',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher'] },
        loadComponent: () =>
          import(
            './features/supervisor-requests/supervisor-requests-list/supervisor-requests-list.component'
          ).then((m) => m.SupervisorRequestsListComponent),
      },
      {
        path: 'supervisor-requests/:id',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher'] },
        loadComponent: () =>
          import(
            './features/supervisor-requests/supervisor-request-detail/supervisor-request-detail.component'
          ).then((m) => m.SupervisorRequestDetailComponent),
      },
      { path: '**', redirectTo: 'topics' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
