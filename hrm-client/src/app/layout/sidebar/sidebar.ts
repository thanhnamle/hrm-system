import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/ui/icon/icon';
import { LayoutState } from '../layout-state';
import { NAV_GROUPS, NavItem } from '../nav-items';

/** "Linh Tran" → "LT", "admin@hrm.local" → "A". */
export function initials(name: string): string {
  const parts = name.split(/[\s@.]+/).filter(Boolean);
  const letters =
    parts.length > 1 && !name.includes('@') ? parts[0][0] + parts.at(-1)![0] : parts[0]?.[0];
  return (letters ?? '?').toUpperCase();
}

@Component({
  imports: [RouterLink, RouterLinkActive, Icon],
  selector: 'app-sidebar',
  host: { class: 'flex h-full flex-col' },
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly auth = inject(AuthService);
  protected readonly layout = inject(LayoutState);

  /** Only the pages this user may open, grouped; empty groups disappear. */
  protected readonly groups = computed(() => {
    this.auth.user(); // dependency: re-filter on login/logout
    return NAV_GROUPS.map((g) => ({
      label: g.label,
      items: g.items.filter((item) => this.allowed(item)),
    })).filter((g) => g.items.length);
  });

  protected readonly user = computed(() => {
    const u = this.auth.user();
    if (!u) return null;
    const name = u.fullName ?? u.email;
    return { name, role: u.role, initials: initials(name), employeeId: u.employeeId };
  });

  protected badge(item: NavItem): number {
    return item.badge === 'pendingApprovals' ? this.layout.pendingApprovals() : 0;
  }

  private allowed(item: NavItem): boolean {
    return (
      (!item.permission || this.auth.hasPermission(item.permission)) &&
      (!item.selfService || this.auth.hasEmployeeRecord())
    );
  }
}
