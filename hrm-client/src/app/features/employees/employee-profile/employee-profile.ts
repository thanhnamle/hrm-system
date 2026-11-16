import { DatePipe } from '@angular/common';
import { HttpResourceRef } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import {
  EmployeeProfile,
  MAX_CV_BYTES,
  Qualification,
  QualificationType,
} from '../../../core/models/employee-profile';
import { ToastService } from '../../../core/notifications/toast.service';
import { FormField } from '../../../shared/forms/form-field';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { ConfirmService } from '../../../shared/ui/confirm/confirm.service';
import { Modal } from '../../../shared/ui/modal/modal';
import { EmployeesApi } from '../employees.api';

const ALREADY_SHOWN = () => undefined; // the error interceptor already showed a toast

const SECTIONS: { type: QualificationType; title: string; empty: string }[] = [
  { type: 'Education', title: 'Education', empty: 'No education added.' },
  { type: 'Certificate', title: 'Certificates', empty: 'No certificates added.' },
  { type: 'PreviousJob', title: 'Previous jobs', empty: 'No previous jobs added.' },
];

/**
 * Career at the company (job history) and CV (skills, qualifications, PDF).
 * Everyone who can open the employee sees it; `editable` (HR/Admin, active employee) adds the controls.
 */
@Component({
  imports: [DatePipe, ReactiveFormsModule, FormField, Modal],
  selector: 'app-employee-profile',
  templateUrl: './employee-profile.html',
})
export class EmployeeProfilePanel {
  private readonly api = inject(EmployeesApi);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly employeeId = input.required<string>();
  readonly profile = input.required<HttpResourceRef<EmployeeProfile | undefined>>();
  readonly editable = input(false);

  protected readonly sections = SECTIONS;
  protected readonly maxCvMb = MAX_CV_BYTES / 1024 / 1024;

  protected readonly data = computed(() => {
    const ref = this.profile();
    return ref.hasValue() ? ref.value() : undefined;
  });

  protected byType(type: QualificationType): Qualification[] {
    return this.data()?.qualifications.filter((q) => q.type === type) ?? [];
  }

  // --- Skills ---
  protected readonly editingSkills = signal(false);
  protected readonly skillsText = signal('');

  protected editSkills(): void {
    this.skillsText.set(this.data()?.skills.join(', ') ?? '');
    this.editingSkills.set(true);
  }

  protected saveSkills(): void {
    const skills = this.skillsText()
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    this.api.updateSkills(this.employeeId(), skills).subscribe({
      next: () => {
        this.editingSkills.set(false);
        this.toast.success('Skills saved.');
        this.profile().reload();
      },
      error: ALREADY_SHOWN,
    });
  }

  // --- Qualifications (one modal for add and edit) ---
  /** null = closed, a type = add one of that type, an entry = edit it. */
  protected readonly editing = signal<QualificationType | Qualification | null>(null);
  protected readonly savingQualification = signal(false);
  protected readonly qualificationErrors = signal<string[]>([]);
  protected readonly qualificationForm = inject(FormBuilder).nonNullable.group({
    type: ['Education' as QualificationType, Validators.required],
    title: ['', [Validators.required, Validators.maxLength(150)]],
    organization: ['', [Validators.required, Validators.maxLength(150)]],
    startDate: [''],
    endDate: [''],
    description: ['', Validators.maxLength(1000)],
  });

  protected modalTitle(): string {
    const editing = this.editing();
    return typeof editing === 'object' && editing ? 'Edit entry' : 'Add entry';
  }

  protected openAdd(type: QualificationType): void {
    this.qualificationForm.reset({
      type,
      title: '',
      organization: '',
      startDate: '',
      endDate: '',
      description: '',
    });
    this.qualificationErrors.set([]);
    this.editing.set(type);
  }

  protected openEdit(q: Qualification): void {
    this.qualificationForm.reset({
      type: q.type,
      title: q.title,
      organization: q.organization,
      startDate: q.startDate ?? '',
      endDate: q.endDate ?? '',
      description: q.description ?? '',
    });
    this.qualificationErrors.set([]);
    this.editing.set(q);
  }

  protected saveQualification(): void {
    if (this.qualificationForm.invalid) {
      this.qualificationForm.markAllAsTouched();
      return;
    }
    const v = this.qualificationForm.getRawValue();
    const body = {
      type: v.type,
      title: v.title,
      organization: v.organization,
      startDate: v.startDate || null,
      endDate: v.endDate || null,
      description: v.description || null,
    };
    const editing = this.editing();
    const request: Observable<unknown> =
      typeof editing === 'object' && editing
        ? this.api.updateQualification(this.employeeId(), editing.id, body)
        : this.api.addQualification(this.employeeId(), body);

    this.savingQualification.set(true);
    request.subscribe({
      next: () => {
        this.savingQualification.set(false);
        this.editing.set(null);
        this.toast.success('Saved.');
        this.profile().reload();
      },
      error: (error: unknown) => {
        this.savingQualification.set(false);
        this.qualificationErrors.set(applyServerErrors(this.qualificationForm, error) ?? []);
      },
    });
  }

  protected async removeQualification(q: Qualification): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this entry?',
      message: `${q.title} · ${q.organization}`,
      confirmText: 'Delete',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.api.deleteQualification(this.employeeId(), q.id).subscribe({
      next: () => this.profile().reload(),
      error: ALREADY_SHOWN,
    });
  }

  // --- CV ---
  protected readonly uploading = signal(false);

  protected upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // so choosing the same file again still triggers a change
    if (!file) return;

    // Quick checks for a friendly message; the API checks the actual bytes.
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      this.toast.error('The CV must be a PDF file.');
      return;
    }
    if (file.size > MAX_CV_BYTES) {
      this.toast.error(`The CV must be at most ${this.maxCvMb} MB.`);
      return;
    }

    this.uploading.set(true);
    this.api.uploadCv(this.employeeId(), file).subscribe({
      next: () => {
        this.uploading.set(false);
        this.toast.success('CV uploaded.');
        this.profile().reload();
      },
      error: () => this.uploading.set(false),
    });
  }

  protected download(fileName: string): void {
    this.api.downloadCv(this.employeeId()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: ALREADY_SHOWN,
    });
  }

  protected async removeCv(): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete the CV?',
      message: 'The file is removed permanently.',
      confirmText: 'Delete',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.api.deleteCv(this.employeeId()).subscribe({
      next: () => this.profile().reload(),
      error: ALREADY_SHOWN,
    });
  }

  protected size(bytes: number): string {
    return bytes < 1024 * 1024
      ? `${Math.ceil(bytes / 1024)} KB`
      : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  protected changeLabel(type: string): string {
    return (
      {
        Hired: 'Hired',
        PositionChanged: 'Position change',
        Transferred: 'Transfer',
        Terminated: 'Terminated',
      }[type] ?? type
    );
  }
}
