import { Component, computed, effect, input, signal } from '@angular/core';
import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Label + input + error message, with one wording for every validator:
 *
 *   <app-form-field label="Email" for="email" [control]="form.controls.email" required>
 *     <input id="email" class="input" formControlName="email" aria-describedby="email-error" />
 *   </app-form-field>
 *
 * The error appears once the field was touched (or the form was submitted with
 * markAllAsTouched). `server` errors come from applyServerErrors().
 */
@Component({
  selector: 'app-form-field',
  template: `
    <label class="label" [attr.for]="for()">
      {{ label() }}
      @if (required()) {
        <span class="text-danger" aria-hidden="true">*</span>
      }
    </label>
    <ng-content />
    @if (error(); as message) {
      <p class="field-error" [id]="for() + '-error'">{{ message }}</p>
    } @else if (hint()) {
      <p class="mt-1 text-xs text-muted" [id]="for() + '-error'">{{ hint() }}</p>
    }
  `,
  host: { class: 'block' },
})
export class FormField {
  readonly label = input.required<string>();
  readonly for = input.required<string>();
  readonly control = input<AbstractControl | null>(null);
  readonly hint = input<string>();
  readonly required = input(false, { transform: (v: boolean | '') => v !== false });

  /** Bumped on every control event, so the computed error below re-evaluates. */
  private readonly version = signal(0);

  protected readonly error = computed(() => {
    this.version();
    const control = this.control();
    return control && control.invalid && control.touched
      ? errorMessage(control.errors, this.label())
      : null;
  });

  constructor() {
    // Controls are not signals; their `events` stream (value, status, touched) drives the view.
    effect((onCleanup) => {
      const subscription = this.control()?.events.subscribe(() =>
        this.version.update((v) => v + 1),
      );
      onCleanup(() => subscription?.unsubscribe());
    });
  }
}

export function errorMessage(errors: ValidationErrors | null, label: string): string | null {
  if (!errors) return null;
  if (errors['server']) return errors['server'] as string;
  if (errors['required']) return `${label} is required.`;
  if (errors['email']) return 'Enter a valid email address.';
  if (errors['maxlength'])
    return `${label} must be at most ${errors['maxlength'].requiredLength} characters.`;
  if (errors['minlength'])
    return `${label} must be at least ${errors['minlength'].requiredLength} characters.`;
  if (errors['min']) return `${label} must be at least ${errors['min'].min}.`;
  if (errors['max']) return `${label} must be at most ${errors['max'].max}.`;
  if (errors['pattern']) return `${label} has an invalid format.`;
  // Custom validators can return { message: '...' }.
  return (errors['message'] as string | undefined) ?? `${label} is invalid.`;
}
