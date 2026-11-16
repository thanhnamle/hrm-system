import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ToastService } from '../../../core/notifications/toast.service';
import { todayIso } from '../../../shared/forms/dates';
import { FormField } from '../../../shared/forms/form-field';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { Loading } from '../../../shared/ui/states/states';
import { EmployeesApi } from '../employees.api';

/** Same rule as the API's EmployeeRules.PhoneRules. */
const PHONE_PATTERN = /^\+?[0-9 ()-]{6,20}$/;

/** Create (/employees/new) and edit (/employees/:id/edit) share this form. */
@Component({
  imports: [ReactiveFormsModule, RouterLink, FormField, PageHeader, Loading],
  selector: 'app-employee-form',
  templateUrl: './employee-form.html',
})
export class EmployeeForm {
  private readonly api = inject(EmployeesApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  /** Route parameter :id; undefined on /employees/new. */
  readonly id = input<string>();
  protected readonly isEdit = computed(() => !!this.id());
  /** Back to the employee when editing, to the list when creating. */
  protected readonly backLink = computed(() =>
    this.id() ? ['/employees', this.id()] : ['/employees'],
  );

  protected readonly employee = this.api.get(() => this.id());
  protected readonly departments = this.api.departments();
  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);

  // Validators mirror the API's FluentValidation rules, so most mistakes never reach the server.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    position: ['', [Validators.required, Validators.maxLength(100)]],
    departmentId: ['', Validators.required],
    hireDate: [todayIso(), Validators.required],
    phone: ['', [Validators.maxLength(20), Validators.pattern(PHONE_PATTERN)]],
    dateOfBirth: [''],
    changeNote: ['', Validators.maxLength(500)],
  });

  constructor() {
    // Edit mode: fill the form once the employee has loaded. The hire date can't change.
    effect(() => {
      if (!this.isEdit() || !this.employee.hasValue()) return;
      const e = this.employee.value();
      this.form.reset({
        firstName: e.firstName,
        lastName: e.lastName,
        email: e.email,
        position: e.position,
        departmentId: e.departmentId,
        hireDate: e.hireDate,
        phone: e.phone ?? '',
        dateOfBirth: e.dateOfBirth ?? '',
        changeNote: '',
      });
      this.form.controls.hireDate.disable();
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.formErrors.set([]);

    const v = this.form.getRawValue();
    const body = {
      firstName: v.firstName,
      lastName: v.lastName,
      email: v.email,
      position: v.position,
      departmentId: v.departmentId,
      phone: v.phone || null,
      dateOfBirth: v.dateOfBirth || null,
    };
    const changeNote = v.changeNote || null;
    const id = this.id();
    const request: Observable<{ id: string } | void> = id
      ? this.api.update(id, { ...body, changeNote })
      : this.api.create({ ...body, hireDate: v.hireDate });

    request.subscribe({
      next: (created) => {
        const targetId = id ?? (created as { id: string }).id;
        this.toast.success(id ? 'Employee updated.' : `${v.firstName} ${v.lastName} was added.`);
        void this.router.navigate(['/employees', targetId]);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // 400: field errors go next to the inputs; anything else was already shown as a toast.
        this.formErrors.set(applyServerErrors(this.form, error) ?? []);
      },
    });
  }
}
