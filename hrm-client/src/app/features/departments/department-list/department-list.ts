import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { Permissions } from '../../../core/auth/permissions';
import { Department } from '../../../core/models/department';
import { ToastService } from '../../../core/notifications/toast.service';
import { FormField } from '../../../shared/forms/form-field';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { HasPermission } from '../../../shared/directives/has-permission.directive';
import { ConfirmService } from '../../../shared/ui/confirm/confirm.service';
import { Modal } from '../../../shared/ui/modal/modal';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { EmptyState, Loading } from '../../../shared/ui/states/states';
import { DepartmentsApi } from '../departments.api';

/**
 * The error interceptor already showed the API's message as a toast. Without an error callback
 * the failed Observable would also reach Angular's global error handler as an unhandled error.
 */
const ALREADY_SHOWN = () => undefined;

/** Everyone can read departments; HR and Admin manage them (create, edit, delete, manager). */
@Component({
  imports: [
    ReactiveFormsModule,
    RouterLink,
    FormField,
    HasPermission,
    Modal,
    PageHeader,
    Loading,
    EmptyState,
  ],
  selector: 'app-department-list',
  templateUrl: './department-list.html',
})
export class DepartmentList {
  private readonly api = inject(DepartmentsApi);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  protected readonly Permissions = Permissions;

  protected readonly departments = this.api.list();
  protected readonly canManage = computed(() => {
    this.auth.user();
    return this.auth.hasPermission(Permissions.DepartmentsManage);
  });
  protected readonly canViewEmployees = computed(() => {
    this.auth.user();
    return this.auth.hasPermission(Permissions.EmployeesView);
  });

  // --- Create / edit (one modal) ---
  /** null = closed, 'new' = create, a department = edit it. */
  protected readonly editing = signal<Department | 'new' | null>(null);
  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    code: [
      '',
      [Validators.required, Validators.maxLength(20), Validators.pattern(/^[A-Za-z0-9_-]+$/)],
    ],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
  });

  // --- Manager ---
  protected readonly managing = signal<Department | null>(null);
  protected readonly members = this.api.members(() => this.managing()?.id);
  protected readonly managerId = new FormControl('', {
    nonNullable: true,
    validators: Validators.required,
  });

  protected openCreate(): void {
    this.form.reset();
    this.form.controls.code.enable();
    this.formErrors.set([]);
    this.editing.set('new');
  }

  protected openEdit(department: Department): void {
    this.form.reset({
      code: department.code,
      name: department.name,
      description: department.description ?? '',
    });
    this.form.controls.code.disable(); // the code is fixed once created
    this.formErrors.set([]);
    this.editing.set(department);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const editing = this.editing();
    const { code, name, description } = this.form.getRawValue();
    const request: Observable<unknown> =
      editing && editing !== 'new'
        ? this.api.update(editing.id, { name, description: description || null })
        : this.api.create({ code, name, description: description || null });

    this.saving.set(true);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing.set(null);
        this.toast.success(
          editing === 'new' ? `Department ${code.toUpperCase()} created.` : 'Department updated.',
        );
        this.departments.reload();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formErrors.set(applyServerErrors(this.form, error) ?? []);
      },
    });
  }

  protected async remove(department: Department): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: `Delete ${department.name}?`,
      message: 'Only possible when no employee record belongs to it. This cannot be undone.',
      confirmText: 'Delete',
      tone: 'danger',
    });
    if (!confirmed) return;

    // 422 "still has N employee record(s)" is shown by the error interceptor.
    this.api.delete(department.id).subscribe({
      next: () => {
        this.toast.success(`${department.name} was deleted.`);
        this.departments.reload();
      },
      error: ALREADY_SHOWN,
    });
  }

  protected openManager(department: Department): void {
    this.managerId.reset(department.managerId ?? '');
    this.managing.set(department);
  }

  protected saveManager(): void {
    const department = this.managing();
    if (!department || this.managerId.invalid) {
      this.managerId.markAsTouched();
      return;
    }
    this.api.assignManager(department.id, this.managerId.value).subscribe({
      next: () => {
        this.managing.set(null);
        this.toast.success(`Manager of ${department.name} updated.`);
        this.departments.reload();
      },
      error: ALREADY_SHOWN,
    });
  }

  protected async removeManager(department: Department): Promise<void> {
    this.managing.set(null); // close the manager dialog; the confirm dialog takes over
    const confirmed = await this.confirm.ask({
      title: `Remove the manager of ${department.name}?`,
      message: `${department.managerName} will no longer approve this department's leave. HR can still approve it.`,
      confirmText: 'Remove manager',
      tone: 'danger',
    });
    if (!confirmed) return;

    this.api.removeManager(department.id).subscribe({
      next: () => {
        this.toast.success(`${department.name} has no manager now.`);
        this.departments.reload();
      },
      error: ALREADY_SHOWN,
    });
  }
}
