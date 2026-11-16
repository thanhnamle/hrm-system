import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { Permissions } from '../../../core/auth/permissions';
import { LeaveRequest, LeaveStatus } from '../../../core/models/leave';
import { ToastService } from '../../../core/notifications/toast.service';
import { FormField } from '../../../shared/forms/form-field';
import { Badge } from '../../../shared/ui/badge/badge';
import { Modal } from '../../../shared/ui/modal/modal';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { LeaveApi } from '../leave.api';

interface Review {
  request: LeaveRequest;
  action: 'approve' | 'reject';
}

/**
 * Pending requests the caller may review (own departments for a Manager, everyone for HR/Admin),
 * plus, for Leave.ViewAll, every request with filters. Nobody reviews their own: the API leaves
 * them out of the inbox and refuses it anyway.
 */
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
  selector: 'app-approvals',
  templateUrl: './approvals.html',
})
export class Approvals {
  private readonly api = inject(LeaveApi);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  protected readonly canViewAll = computed(() => {
    this.auth.user();
    return this.auth.hasPermission(Permissions.LeaveViewAll);
  });
  /** The system admin account has no employee record, so the API can't record it as reviewer. */
  protected readonly canReview = this.auth.hasEmployeeRecord;

  protected readonly tab = signal<'pending' | 'all'>('pending');

  protected readonly pendingPage = signal(1);
  protected readonly pending = this.api.pending(() => ({ page: this.pendingPage(), pageSize: 20 }));

  protected readonly allStatus = signal<LeaveStatus | ''>('');
  protected readonly allDepartment = signal('');
  protected readonly allPage = linkedSignal({
    source: () => [this.allStatus(), this.allDepartment()],
    computation: () => 1,
  });
  protected readonly all = this.api.all(() =>
    this.canViewAll() && this.tab() === 'all'
      ? {
          status: this.allStatus() || undefined,
          departmentId: this.allDepartment() || undefined,
          page: this.allPage(),
          pageSize: 20,
        }
      : undefined,
  );
  protected readonly departments = this.api.departments();

  // --- Review dialog ---
  protected readonly reviewing = signal<Review | null>(null);
  protected readonly saving = signal(false);
  protected readonly note = new FormControl('', {
    nonNullable: true,
    validators: Validators.maxLength(500),
  });

  protected open(request: LeaveRequest, action: Review['action']): void {
    // A rejection always needs a reason the employee can read; approval notes are optional.
    this.note.setValidators(
      action === 'reject'
        ? [Validators.required, Validators.maxLength(500)]
        : Validators.maxLength(500),
    );
    this.note.reset('');
    this.reviewing.set({ request, action });
  }

  protected confirmReview(): void {
    const review = this.reviewing();
    if (!review) return;
    if (this.note.invalid) {
      this.note.markAsTouched();
      return;
    }
    const note = this.note.value.trim();
    const request =
      review.action === 'approve'
        ? this.api.approve(review.request.id, note || null)
        : this.api.reject(review.request.id, note);

    this.saving.set(true);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.reviewing.set(null);
        this.toast.success(
          `${review.request.employeeName}'s request was ${review.action === 'approve' ? 'approved' : 'rejected'}.`,
        );
        this.pending.reload();
        if (this.tab() === 'all') this.all.reload();
      },
      error: () => this.saving.set(false), // e.g. 409 when someone else reviewed it first: toast
    });
  }
}
