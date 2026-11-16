import { DatePipe } from '@angular/common';
import { Component, inject, input, linkedSignal, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Permissions } from '../../../core/auth/permissions';
import { EmployeeStatus } from '../../../core/models/employee';
import { HasPermission } from '../../../shared/directives/has-permission.directive';
import { Badge } from '../../../shared/ui/badge/badge';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Pagination } from '../../../shared/ui/pagination/pagination';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { EmployeesApi } from '../employees.api';

@Component({
  imports: [
    RouterLink,
    DatePipe,
    PageHeader,
    Badge,
    Pagination,
    Loading,
    EmptyState,
    HasPermission,
  ],
  selector: 'app-employee-list',
  templateUrl: './employee-list.html',
})
export class EmployeeList {
  private readonly api = inject(EmployeesApi);
  protected readonly Permissions = Permissions;
  protected readonly pageSize = 20;

  /** ?departmentId=… when coming from the Departments page. */
  readonly departmentId = input<string>();

  protected readonly searchText = signal('');
  /** The API is called 300 ms after typing stops, not on every key. */
  private readonly search = toSignal(
    toObservable(this.searchText).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );
  protected readonly department = linkedSignal(() => this.departmentId() ?? '');
  protected readonly status = signal<EmployeeStatus | ''>('Active');
  /** Back to page 1 whenever a filter changes. */
  protected readonly page = linkedSignal({
    source: () => [this.search(), this.department(), this.status()],
    computation: () => 1,
  });

  protected readonly employees = this.api.list(() => ({
    search: this.search(),
    departmentId: this.department(),
    status: this.status() || undefined,
    page: this.page(),
    pageSize: this.pageSize,
  }));
  protected readonly departments = this.api.departments();

  protected valueOf(event: Event): string {
    return (event.target as HTMLInputElement | HTMLSelectElement).value;
  }
}
