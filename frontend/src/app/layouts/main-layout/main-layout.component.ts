import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Badge } from 'primeng/badge';
import { Button } from 'primeng/button';

import { AuthService } from '../../core/auth/auth.service';
import { NotificationBadgeService } from '../../core/notifications/notification-badge.service';
import { NAV_ITEMS } from './nav-items';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Button, Badge],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notificationBadge = inject(NotificationBadgeService);

  readonly currentUser = this.auth.currentUser;
  readonly unreadCount = this.notificationBadge.unreadCount;

  readonly navItems = computed(() => {
    const role = this.auth.role();
    if (!role) return [];
    return NAV_ITEMS.filter((item) => item.roles.includes(role));
  });
  readonly canSeeNotifications = computed(() => {
    const role = this.auth.role();
    return role !== null && role !== 'Admin';
  });

  ngOnInit(): void {
    this.notificationBadge.startPolling(this.auth.role());
  }

  logout(): void {
    this.auth.logout().subscribe(() => {
      this.notificationBadge.reset();
      void this.router.navigateByUrl('/login');
    });
  }
}
