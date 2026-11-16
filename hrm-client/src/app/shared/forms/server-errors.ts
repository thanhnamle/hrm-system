import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ProblemDetails } from '../../core/models/common';

/**
 * Puts the API's 400 validation errors next to the matching inputs.
 * The API names fields like its C# properties ("FirstName"); form controls use camelCase
 * ("firstName"). Each message is stored as the control's `server` error, which
 * <app-form-field> displays and which clears as soon as the user edits the field.
 *
 * Returns the messages that matched no control (show them above the form), or null if
 * the error was not a validation error at all (let the caller or the toast handle it).
 */
export function applyServerErrors(form: FormGroup, error: unknown): string[] | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) return null;

  const problem = error.error as ProblemDetails | null;
  const unmatched: string[] = [];

  for (const [field, messages] of Object.entries(problem?.errors ?? {})) {
    const control = form.get(field.charAt(0).toLowerCase() + field.slice(1));
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched.length || problem?.errors
    ? unmatched
    : [problem?.detail ?? 'Please check the form.'];
}
