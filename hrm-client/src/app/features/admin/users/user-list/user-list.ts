import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { Role } from '../../../../core/auth/permissions';
import { UserAccount } from '../../../../core/models/user';
import { ToastService } from '../../../../core/notifications/toast.service';
import { FormField } from '../../../../shared/forms/form-field';
import {
  generatePassword,
  PASSWORD_HINT,
  passwordPolicy,
} from '../../../../shared/forms/passwords';
import { applyServerErrors } from '../../../../shared/forms/server-errors';
import { Badge } from '../../../../shared/ui/badge/badge';
import { ConfirmService } from '../../../../shared/ui/confirm/confirm.service';
import { Modal } from '../../../../shared/ui/modal/modal';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { EmptyState, Loading } from '../../../../shared/ui/states/states';
import { UsersApi } from '../../users.api';

const ALREADY_SHOWN = () => undefined; // the error interceptor already showed a toast

/**
 * Login accounts. Changing a role, locking or resetting a password signs that user out of every
 * session (the API revokes their refresh tokens). Admins can't change their own role or lock
 * themselves: the API refuses it, and the actions are hidden on their own row.
 */
@Component({
  imports: [ReactiveFormsModule, FormField, Badge, Modal, PageHeader, Loading, EmptyState],
  selector: 'app-user-list',
  templateUrl: './user-list.html',
})
export class UserList {
  private readonly api = inject(UsersApi);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly roles: Role[] = ['Employee', 'Manager', 'HR', 'Admin'];
  protected readonly passwordHint = PASSWORD_HINT;
  protected readonly myId = computed(() => this.auth.user()?.id);

  protected readonly users = this.api.list();
  private readonly employees = this.api.activeEmployees();
  /** Active employees who don't have a login yet. */
  protected readonly withoutLogin = computed(() => {
    if (!this.employees.hasValue() || !this.users.hasValue()) return [];
    const taken = new Set(this.users.value().map((u) => u.employeeId));
    return this.employees.value().items.filter((e) => !taken.has(e.id));
  });

  // --- Create login ---
  protected readonly creating = signal(false);
  protected readonly saving = signal(false);
  protected readonly createErrors = signal<string[]>([]);
  protected readonly createForm = inject(FormBuilder).nonNullable.group({
    employeeId: ['', Validators.required],
    role: ['Employee' as Role, Validators.required],
    password: ['', [Validators.required, Validators.maxLength(128), passwordPolicy]],
  });

  protected openCreate(): void {
    this.createForm.reset({ employeeId: '', role: 'Employee', password: generatePassword() });
    this.createErrors.set([]);
    this.creating.set(true);
  }

  protected newPassword(control: FormControl<string>): void {
    control.setValue(generatePassword());
    control.markAsTouched();
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const value = this.createForm.getRawValue();
    this.saving.set(true);
    this.api.create(value).subscribe({
      next: () => {
        this.saving.set(false);
        this.creating.set(false);
        const employee = this.employees.value()?.items.find((e) => e.id === value.employeeId);
        this.toast.success(
          `Login created for ${employee?.fullName ?? 'the employee'}. Share the password securely.`,
        );
        this.users.reload();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.createErrors.set(applyServerErrors(this.createForm, error) ?? []);
      },
    });
  }

  // --- Change role ---
  protected readonly changingRole = signal<UserAccount | null>(null);
  protected readonly roleControl = new FormControl<Role>('Employee', { nonNullable: true });

  protected openRole(user: UserAccount): void {
    this.roleControl.setValue(user.role);
    this.changingRole.set(user);
  }

  protected saveRole(): void {
    const user = this.changingRole();
    if (!user) return;
    this.api.changeRole(user.id, this.roleControl.value).subscribe({
      next: () => {
        this.changingRole.set(null);
        this.toast.success(
          `${this.name(user)} is now ${this.roleControl.value}. They will sign in again with the new role.`,
        );
        this.users.reload();
      },
      error: ALREADY_SHOWN,
    });
  }

  // --- Lock / unlock ---
  protected async toggleLock(user: UserAccount): Promise<void> {
    if (user.isLockedOut) {
      this.api.unlock(user.id).subscribe({
        next: () => {
          this.toast.success(`${this.name(user)} can sign in again.`);
          this.users.reload();
        },
        error: ALREADY_SHOWN,
      });
      return;
    }
    const confirmed = await this.confirm.ask({
      title: `Lock ${this.name(user)}?`,
      message: 'They are signed out everywhere and cannot sign in until you unlock the account.',
      confirmText: 'Lock account',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.api.lock(user.id).subscribe({
      next: () => {
        this.toast.success(`${this.name(user)} is locked.`);
        this.users.reload();
      },
      error: ALREADY_SHOWN,
    });
  }

  // --- Reset password ---
  protected readonly resetting = signal<UserAccount | null>(null);
  protected readonly resetErrors = signal<string[]>([]);
  protected readonly resetForm = inject(FormBuilder).nonNullable.group({
    newPassword: ['', [Validators.required, Validators.maxLength(128), passwordPolicy]],
  });

  protected openReset(user: UserAccount): void {
    this.resetForm.reset({ newPassword: generatePassword() });
    this.resetErrors.set([]);
    this.resetting.set(user);
  }

  protected saveReset(): void {
    const user = this.resetting();
    if (!user) return;
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }
    this.api.resetPassword(user.id, this.resetForm.getRawValue().newPassword).subscribe({
      next: () => {
        this.resetting.set(null);
        this.toast.success(
          `Password reset for ${this.name(user)}. They were signed out everywhere.`,
        );
      },
      error: (error: unknown) =>
        this.resetErrors.set(applyServerErrors(this.resetForm, error) ?? []),
    });
  }

  protected name(user: UserAccount): string {
    return user.fullName ?? user.email;
  }
}
