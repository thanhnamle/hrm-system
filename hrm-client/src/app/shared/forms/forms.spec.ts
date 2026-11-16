import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { toParams } from '../../core/http/params';
import { errorMessage, FormField } from './form-field';
import { applyServerErrors } from './server-errors';

const badRequest = (errors?: Record<string, string[]>, detail?: string) =>
  new HttpErrorResponse({ status: 400, error: { title: 'Validation failed', detail, errors } });

describe('applyServerErrors', () => {
  const form = () => new FormGroup({ firstName: new FormControl(''), email: new FormControl('') });

  it('puts API field errors (PascalCase) on the camelCase controls', () => {
    const f = form();
    const unmatched = applyServerErrors(
      f,
      badRequest({ FirstName: ['Too long.'], Email: ['Taken.'] }),
    );

    expect(unmatched).toEqual([]);
    expect(f.controls.firstName.errors).toEqual({ server: 'Too long.' });
    expect(f.controls.email.touched).toBe(true);
  });

  it('returns messages for fields the form does not have', () => {
    expect(
      applyServerErrors(form(), badRequest({ DepartmentId: ['Department does not exist.'] })),
    ).toEqual(['Department does not exist.']);
  });

  it('returns null for non-validation errors, so the toast handles them', () => {
    expect(applyServerErrors(form(), new HttpErrorResponse({ status: 409 }))).toBeNull();
  });

  it('clears the server error as soon as the user edits the field', () => {
    const f = form();
    applyServerErrors(f, badRequest({ Email: ['Taken.'] }));

    f.controls.email.setValue('other@hrm.local');

    expect(f.controls.email.errors).toBeNull();
  });
});

describe('errorMessage', () => {
  it.each([
    [{ required: true }, 'Email is required.'],
    [{ email: true }, 'Enter a valid email address.'],
    [{ maxlength: { requiredLength: 100 } }, 'Email must be at most 100 characters.'],
    [{ server: 'Already used.', required: true }, 'Already used.'],
    [{ message: 'Must be a weekday.' }, 'Must be a weekday.'],
  ])('%j → %s', (errors, expected) => {
    expect(errorMessage(errors, 'Email')).toBe(expected);
  });
});

describe('FormField', () => {
  @Component({
    imports: [ReactiveFormsModule, FormField],
    template: `
      <app-form-field label="Email" for="email" [control]="control" required>
        <input id="email" class="input" [formControl]="control" />
      </app-form-field>
    `,
  })
  class Host {
    readonly control = new FormControl('', Validators.required);
  }

  it('shows the error once touched and hides it when fixed, without zone.js', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('.field-error')).toBeNull(); // untouched: no error yet

    fixture.componentInstance.control.markAsTouched();
    await fixture.whenStable();
    expect(page.querySelector('.field-error')?.textContent).toContain('Email is required.');

    fixture.componentInstance.control.setValue('a@hrm.local');
    await fixture.whenStable();
    expect(page.querySelector('.field-error')).toBeNull();
  });
});

describe('toParams', () => {
  it('skips empty values', () => {
    expect(
      toParams({
        search: '',
        status: undefined,
        departmentId: null,
        page: 2,
        pageSize: 20,
      }).toString(),
    ).toBe('page=2&pageSize=20');
  });
});
