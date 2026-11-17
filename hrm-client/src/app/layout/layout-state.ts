import { HttpContext, httpResource } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../core/auth/auth.service';
import { Permissions } from '../core/auth/permissions';
import { SKIP_ERROR_TOAST } from '../core/http/http-context';

/** "Employees · HRM" → "Employees": the route titles double as page headings in the top bar. */
function pageTitle(root: ActivatedRouteSnapshot): string {
  let route = root;
  while (route.firstChild) route = route.firstChild;
  const title = typeof route.title === 'string' ? route.title : '';
  return title.replace(/\s*·\s*HRM$/, '') || 'HR Management System';
}

/**
 * State the shell, sidebar and top bar share. Provided by the shell, so it lives as long as the
 * signed-in layout does.
 */
@Injectable()
export class LayoutState {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  private readonly navigated = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.routerState.snapshot.root),
    ),
    { initialValue: this.router.routerState.snapshot.root },
  );

  /** The current page's name, for the top bar. */
  readonly title = computed(() => pageTitle(this.navigated()));

  /** The sidebar as a drawer on small screens. Closes on every navigation. */
  readonly navOpen = signal(false);

  constructor() {
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => this.navOpen.set(false));
  }

  /**
   * Leave requests waiting for this user's review, for the Approvals badge. Re-read on every
   * navigation, so the count drops right after approving. Silent on errors: it's only a hint.
   */
  private readonly pending = httpResource<{ totalCount: number }>(() => {
    this.navigated();
    if (!this.auth.hasPermission(Permissions.LeaveApprove)) return undefined;
    return {
      url: `${environment.apiBaseUrl}/leave-requests/pending`,
      params: { pageSize: 1 },
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    };
  });

  readonly pendingApprovals = computed(() =>
    this.pending.hasValue() ? this.pending.value().totalCount : 0,
  );
}
