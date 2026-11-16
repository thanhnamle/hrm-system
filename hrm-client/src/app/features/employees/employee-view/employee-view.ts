import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Permissions } from '../../../core/auth/permissions';
import { EmployeeDetail } from '../../../core/models/employee';
import { ToastService } from '../../../core/notifications/toast.service';
import { HasPermission } from '../../../shared/directives/has-permission.directive';
import { Badge } from '../../../shared/ui/badge/badge';
import { ConfirmService } from '../../../shared/ui/confirm/confirm.service';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { EmployeeProfilePanel } from '../employee-profile/employee-profile';
import { EmployeesApi } from '../employees.api';

@Component({
  imports: [
    RouterLink,
    DatePipe,
    DecimalPipe,
    PageHeader,
    Badge,
    Loading,
    EmptyState,
    HasPermission,
    EmployeeProfilePanel,
  ],
  selector: 'app-employee-view',
  templateUrl: './employee-view.html',
})
export class EmployeeView {
  private readonly api = inject(EmployeesApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  protected readonly Permissions = Permissions;

  /** Route parameter :id. */
  readonly id = input.required<string>();

  protected readonly employee = this.api.get(() => this.id());
  /** Account role, job history and CV; loaded alongside the record. */
  protected readonly profile = this.api.profile(() => this.id());
  /** HR/Admin can edit the profile of an active employee. */
  protected readonly profileEditable = computed(() => {
    this.auth.user();
    return (
      this.auth.hasPermission(Permissions.EmployeesUpdate) &&
      this.employee.hasValue() &&
      this.employee.value().status === 'Active'
    );
  });
  protected readonly terminating = signal(false);
  /** 404 also means "outside your data scope": the API doesn't reveal which. */
  protected readonly notFound = computed(
    () => (this.employee.error() as HttpErrorResponse | undefined)?.status === 404,
  );

  protected async terminate(employee: EmployeeDetail): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: `Terminate ${employee.fullName}?`,
      message:
        'They will be signed out and can no longer log in. Pending leave requests are cancelled and, ' +
        'if they manage a department, it will have no manager. The record is kept for history.',
      confirmText: 'Terminate',
      tone: 'danger',
    });
    if (!confirmed) return;

    this.terminating.set(true);
    this.api.terminate(employee.id).subscribe({
      next: () => {
        this.terminating.set(false);
        this.toast.success(`${employee.fullName} was terminated.`);
        this.employee.reload();
        this.profile.reload(); // the timeline gets its Terminated entry
      },
      error: () => this.terminating.set(false), // the error interceptor shows why
    });
  }
}
