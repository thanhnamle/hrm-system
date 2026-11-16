import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NAV_ITEMS } from '../nav-items';

@Component({
  imports: [RouterLink, RouterLinkActive],
  selector: 'app-sidebar',
  templateUrl: './sidebar.html',
})
export class Sidebar {
  private readonly auth = inject(AuthService);

  /** Only the pages this user may open. Recomputed when the signed-in user changes. */
  protected readonly items = computed(() => {
    this.auth.user(); // dependency: re-filter on login/logout
    return NAV_ITEMS.filter(
      (item) =>
        (!item.permission || this.auth.hasPermission(item.permission)) &&
        (!item.selfService || this.auth.hasEmployeeRecord()),
    );
  });
}
