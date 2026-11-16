import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LeaveRequest, LeaveStatus, LeaveType } from '../../../core/models/leave';
import { ToastService } from '../../../core/notifications/toast.service';
import { workingDays } from '../../../shared/forms/dates';
import { FormField } from '../../../shared/forms/form-field';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { Badge } from '../../../shared/ui/badge/badge';
import { ConfirmService } from '../../../shared/ui/confirm/confirm.service';
import { Modal } from '../../../shared/ui/modal/modal';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { LeaveApi } from '../leave.api';

@Component({
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    FormField,
    Badge,
    Modal,
    PageHeader,
    Pagination,
    Loading,
    EmptyState,
  ],
  selector: 'app-my-requests',
  templateUrl: './my-requests.html',
})
export class MyRequests {
  private readonly api = inject(LeaveApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  protected readonly pageSize = 10;

  protected readonly status = signal<LeaveStatus | ''>('');
  protected readonly page = linkedSignal({ source: () => this.status(), computation: () => 1 });
  protected readonly requests = this.api.mine(() => ({
    status: this.status() || undefined,
    page: this.page(),
    pageSize: this.pageSize,
  }));
  protected readonly balance = this.api.balance();

  // --- Request form (modal) ---
  protected readonly formOpen = signal(false);
  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    type: ['Annual' as LeaveType, Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  /** Live preview while picking dates; null until both dates make sense. */
  protected readonly days = computed(() => {
    const { startDate, endDate } = this.formValue();
    return startDate && endDate && endDate >= startDate ? workingDays(startDate, endDate) : null;
  });
  /** Warn before submitting; the API enforces it either way. */
  protected readonly exceedsBalance = computed(() => {
    const days = this.days();
    return (
      this.formValue().type === 'Annual' &&
      days !== null &&
      this.balance.hasValue() &&
      days > this.balance.value().available
    );
  });

  protected openForm(): void {
    this.form.reset({ type: 'Annual', startDate: '', endDate: '', reason: '' });
    this.formErrors.set([]);
    this.formOpen.set(true);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.formErrors.set([]);
    this.api.submit(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.toast.success('Leave request submitted. Your manager will review it.');
        this.refresh();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // 400: fields; 422 (overlap, balance, past date…) is shown as a toast by the interceptor.
        this.formErrors.set(applyServerErrors(this.form, error) ?? []);
      },
    });
  }

  protected async cancel(request: LeaveRequest): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Cancel this leave request?',
      message: `${request.type} leave, ${request.days} day(s). You can submit a new one later.`,
      confirmText: 'Cancel request',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.api.cancel(request.id).subscribe({
      next: () => {
        this.toast.success('Request cancelled.');
        this.refresh();
      },
      error: () => undefined, // already shown as a toast
    });
  }

  private refresh(): void {
    this.requests.reload();
    this.balance.reload();
  }
}
