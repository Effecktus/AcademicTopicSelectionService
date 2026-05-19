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
          import('./features/topics/topic-form/topic-form.component').then((m) => m.TopicFormComponent),
      },
      {
        path: 'topics/:id/edit',
        pathMatch: 'full',
        redirectTo: '/topics/:id',
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
      {
        path: 'notifications',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher', 'DepartmentHead'] },
        loadComponent: () =>
          import('./features/notifications/notifications-list/notifications-list.component').then(
            (m) => m.NotificationsListComponent,
          ),
      },
      {
        path: 'applications',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher', 'DepartmentHead', 'Admin'] },
        loadComponent: () =>
          import('./features/applications/applications-list/applications-list.component').then(
            (m) => m.ApplicationsListComponent,
          ),
      },
      {
        path: 'applications/new',
        canActivate: [roleGuard],
        data: { role: ['Student'] },
        loadComponent: () =>
          import('./features/applications/application-create/application-create.component').then(
            (m) => m.ApplicationCreateComponent,
          ),
      },
      {
        path: 'applications/:id',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher', 'DepartmentHead', 'Admin'] },
        loadComponent: () =>
          import('./features/applications/application-detail/application-detail.component').then(
            (m) => m.ApplicationDetailComponent,
          ),
      },
      {
        path: 'graduate-works',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher', 'DepartmentHead'] },
        loadComponent: () =>
          import('./features/graduate-works/graduate-works-list/graduate-works-list.component').then(
            (m) => m.GraduateWorksListComponent,
          ),
      },
      {
        path: 'graduate-works/:id',
        canActivate: [roleGuard],
        data: { role: ['Student', 'Teacher', 'DepartmentHead'] },
        loadComponent: () =>
          import('./features/graduate-works/graduate-work-detail/graduate-work-detail.component').then(
            (m) => m.GraduateWorkDetailComponent,
          ),
      },
      {
        path: 'department-head/analytics',
        canActivate: [roleGuard],
        data: { role: 'DepartmentHead' },
        loadComponent: () =>
          import('./features/department-head/department-analytics/department-analytics.component').then(
            (m) => m.DepartmentAnalyticsComponent,
          ),
      },
      {
        path: 'admin/users',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () =>
          import('./features/admin/admin-users/admin-users-list.component').then(
            (m) => m.AdminUsersListComponent,
          ),
      },
      {
        path: 'admin/graduate-works',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () =>
          import('./features/admin/admin-graduate-works/admin-gw-list.component').then(
            (m) => m.AdminGwListComponent,
          ),
      },
      {
        path: 'admin/analytics',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () =>
          import('./features/admin/admin-analytics/admin-analytics.component').then(
            (m) => m.AdminAnalyticsComponent,
          ),
      },
      {
        path: 'admin/export',
        canActivate: [roleGuard],
        data: { role: 'Admin' },
        loadComponent: () =>
          import('./features/admin/admin-export/admin-export.component').then(
            (m) => m.AdminExportComponent,
          ),
      },
      { path: '**', redirectTo: 'topics' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
