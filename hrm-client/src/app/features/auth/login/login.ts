import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { messageFor } from '../../../core/http/error.interceptor';
import { environment } from '../../../../environments/environment';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-login',
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** ?returnUrl=... from authGuard, bound by withComponentInputBinding(). */
  readonly returnUrl = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showDemoAccounts = !environment.production;
  protected readonly demoAccounts = [
    { role: 'Admin', email: 'admin@hrm.local' },
    { role: 'HR', email: 'linh.tran@hrm.local' },
    { role: 'Manager', email: 'minh.nguyen@hrm.local' },
    { role: 'Employee', email: 'huy.le@hrm.local' },
  ];

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(this.safeReturnUrl()),
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        // 401 covers wrong password, unknown email and locked account alike (no account enumeration).
        this.error.set(err.status === 401 ? 'Invalid email or password.' : messageFor(err));
      },
    });
  }

  protected useDemo(email: string): void {
    this.form.patchValue({ email });
  }

  protected invalid(field: 'email' | 'password'): boolean {
    const control = this.form.controls[field];
    return control.invalid && control.touched;
  }

  /** Only same-app paths, so a crafted link like ?returnUrl=https://evil.example can't redirect away. */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/login')
      ? url
      : '/dashboard';
  }
}
