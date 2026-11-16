import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Badge } from '../../../shared/ui/badge/badge';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { DashboardApi } from '../dashboard.api';

/**
 * Home page. Admin/HR see the organization, a Manager their team, and anyone with an employee
 * record their own leave and attendance. The API decides which sections are filled.
 */
@Component({
  imports: [DatePipe, DecimalPipe, RouterLink, Badge, Loading, EmptyState],
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly auth = inject(AuthService);
  protected readonly summary = inject(DashboardApi).summary();

  protected readonly firstName = computed(() => {
    const user = this.auth.user();
    return user?.fullName?.split(' ')[0] ?? 'there';
  });

  /** Bars are scaled to the largest department, so the longest bar fills the track. */
  protected readonly departments = computed(() => {
    if (!this.summary.hasValue()) return [];
    const rows = this.summary.value().organization?.headcountByDepartment ?? [];
    const max = Math.max(1, ...rows.map((d) => d.headcount));
    return rows.map((d) => ({ ...d, percent: (d.headcount / max) * 100 }));
  });
}
