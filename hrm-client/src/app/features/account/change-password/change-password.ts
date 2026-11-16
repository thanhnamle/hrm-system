import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { ToastService } from '../../../core/notifications/toast.service';
import { FormField } from '../../../shared/forms/form-field';
import { PASSWORD_HINT, passwordPolicy } from '../../../shared/forms/passwords';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { AccountApi } from '../account.api';

/** Checked on the "repeat" field, so the error shows next to it. */
const matchesNewPassword = (control: AbstractControl): ValidationErrors | null =>
  control.value && control.value !== control.parent?.get('newPassword')?.value
    ? { message: 'The passwords do not match.' }
    : null;

/**
 * Any signed-in user. The API then revokes every session of the account (so a stolen session
 * stops working too), which is why the user signs in again afterwards.
 */
@Component({
  imports: [ReactiveFormsModule, FormField, PageHeader],
  selector: 'app-change-password',
  templateUrl: './change-password.html',
})
export class ChangePassword {
  private readonly api = inject(AccountApi);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  protected readonly hint = PASSWORD_HINT;
  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.maxLength(128), passwordPolicy]],
    repeatPassword: ['', [Validators.required, matchesNewPassword]],
  });

  constructor() {
    // Re-check the repeat field when the new password changes.
    this.form.controls.newPassword.valueChanges.subscribe(() =>
      this.form.controls.repeatPassword.updateValueAndValidity({ emitEvent: false }),
    );
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword } = this.form.getRawValue();
    this.saving.set(true);
    this.formErrors.set([]);
    this.api.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.toast.success('Password changed. Please sign in with your new password.');
        this.auth.logout();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        // e.g. 400 "Current password is incorrect." shown next to that field
        this.formErrors.set(applyServerErrors(this.form, error) ?? []);
      },
    });
  }
}
