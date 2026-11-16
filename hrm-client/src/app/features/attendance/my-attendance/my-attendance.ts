import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { AttendanceRecord } from '../../../core/models/attendance';
import { ToastService } from '../../../core/notifications/toast.service';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { AttendanceApi } from '../attendance.api';

/**
 * Check in when you start, check out when you leave. The server records the time and decides
 * which day it is (company time zone), so the browser's clock never matters.
 */
@Component({
  imports: [DatePipe, DecimalPipe, PageHeader, Pagination, Loading, EmptyState],
  providers: [DatePipe], // also used in code, for the toast message
  selector: 'app-my-attendance',
  templateUrl: './my-attendance.html',
})
export class MyAttendance {
  private readonly api = inject(AttendanceApi);
  private readonly toast = inject(ToastService);
  private readonly datePipe = inject(DatePipe);

  protected readonly today = this.api.today();
  protected readonly page = signal(1);
  protected readonly history = this.api.mine(() => ({ page: this.page(), pageSize: 10 }));
  protected readonly busy = signal(false);

  /** 'none' → can check in, 'in' → can check out, 'out' → done for today. */
  protected readonly state = computed(() => {
    if (!this.today.hasValue()) return null;
    const record = this.today.value().record;
    return !record ? 'none' : record.checkOutAt ? 'out' : 'in';
  });

  protected checkIn(): void {
    this.run(this.api.checkIn(), (r) => `Checked in at ${this.time(r.checkInAt)}.`);
  }

  protected checkOut(): void {
    this.run(this.api.checkOut(), (r) => `Checked out at ${this.time(r.checkOutAt ?? '')}.`);
  }

  private run(
    request: Observable<AttendanceRecord>,
    message: (record: AttendanceRecord) => string,
  ): void {
    this.busy.set(true);
    request.subscribe({
      next: (record) => {
        this.busy.set(false);
        this.toast.success(message(record));
        this.today.reload();
        this.history.reload();
      },
      error: () => this.busy.set(false), // 409/422 (already done today) is shown as a toast
    });
  }

  private time(value: string): string {
    return this.datePipe.transform(value, 'shortTime') ?? value;
  }
}
