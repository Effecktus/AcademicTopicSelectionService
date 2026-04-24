import type { Role } from '../../core/models/auth.models';

export interface NavItem {
    label: string;
    icon: string;
    route: string;
    roles: Role[];
}

export const NAV_ITEMS: NavItem[] = [
    { label: 'Преподаватели', icon: 'pi pi-users', route: '/teachers', roles: ['Student', 'Teacher', 'DepartmentHead'] },
    { label: 'Темы', icon: 'pi pi-book', route: '/topics', roles: ['Student', 'Teacher', 'DepartmentHead'] },
    { label: 'Мои запросы', icon: 'pi pi-send', route: '/supervisor-requests', roles: ['Student'] },
    { label: 'Мои заявки', icon: 'pi pi-file-edit', route: '/applications', roles: ['Student'] },
    { label: 'Запросы (вх.)', icon: 'pi pi-inbox', route: '/supervisor-requests', roles: ['Teacher'] },
    { label: 'Заявки', icon: 'pi pi-file-edit', route: '/applications', roles: ['Teacher', 'DepartmentHead'] },
    { label: 'Архив ВКР', icon: 'pi pi-server', route: '/graduate-works', roles: ['Student', 'Teacher', 'DepartmentHead'] },
    { label: 'Пользователи', icon: 'pi pi-user-edit', route: '/admin/users', roles: ['Admin'] },
    { label: 'Архив ВКР', icon: 'pi pi-server', route: '/admin/graduate-works', roles: ['Admin'] },
    { label: 'Аналитика', icon: 'pi pi-chart-bar', route: '/admin/analytics', roles: ['Admin'] },
    { label: 'Экспорт', icon: 'pi pi-download', route: '/admin/export', roles: ['Admin'] },
];