import { DatePipe } from '@angular/common';
import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { TeamAttendanceStatus } from '../../../core/models/attendance';
import { Badge } from '../../../shared/ui/badge/badge';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { AttendanceApi } from '../attendance.api';

/** Who is in, out or on leave on a day: everyone for HR/Admin, own departments for a Manager. */
@Component({
  imports: [DatePipe, Badge, PageHeader, Pagination, Loading, EmptyState],
  selector: 'app-team-attendance',
  templateUrl: './team-attendance.html',
})
export class TeamAttendance {
  private readonly api = inject(AttendanceApi);
  private readonly auth = inject(AuthService);

  /** '' = today (decided by the server). */
  protected readonly date = signal('');
  protected readonly department = signal('');
  protected readonly page = linkedSignal({
    source: () => [this.date(), this.department()],
    computation: () => 1,
  });
  protected readonly team = this.api.team(() => ({
    date: this.date() || undefined,
    departmentId: this.department() || undefined,
    page: this.page(),
    pageSize: 50,
  }));

  /** Managers only see their own departments anyway, so the filter is for HR and Admin. */
  protected readonly showDepartmentFilter = computed(() =>
    ['Admin', 'HR'].includes(this.auth.user()?.role ?? ''),
  );
  protected readonly departments = this.api.departments();

  /** Counts for the rows on this page (the whole team unless it has more than 50 people). */
  protected readonly summary = computed(() => {
    const counts: Record<TeamAttendanceStatus, number> = {
      Present: 0,
      CheckedOut: 0,
      OnLeave: 0,
      Absent: 0,
    };
    if (this.team.hasValue()) {
      for (const row of this.team.value().items) counts[row.status]++;
    }
    return counts;
  });
}
