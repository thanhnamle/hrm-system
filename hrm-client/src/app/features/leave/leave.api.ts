import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { toParams } from '../../core/http/params';
import { CreatedResponse, PagedResult } from '../../core/models/common';
import { Department } from '../../core/models/department';
import {
  LeaveBalance,
  LeaveQuery,
  LeaveRequest,
  MyLeaveQuery,
  SubmitLeaveRequest,
} from '../../core/models/leave';

/** Every call the Leave feature makes. Reads are resources (see EmployeesApi), writes Observables. */
@Injectable({ providedIn: 'root' })
export class LeaveApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/leave-requests`;

  mine(query: () => MyLeaveQuery) {
    return httpResource<PagedResult<LeaveRequest>>(() => ({
      url: `${this.url}/mine`,
      params: toParams(query()),
    }));
  }

  balance() {
    return httpResource<LeaveBalance>(() => `${this.url}/balance`);
  }

  /** Requests the caller can review: their departments for a Manager, everyone for HR/Admin. */
  pending(query: () => MyLeaveQuery) {
    return httpResource<PagedResult<LeaveRequest>>(() => ({
      url: `${this.url}/pending`,
      params: toParams(query()),
    }));
  }

  /** Everyone's requests (Leave.ViewAll). Pass undefined to skip loading for other roles. */
  all(query: () => LeaveQuery | undefined) {
    return httpResource<PagedResult<LeaveRequest>>(() => {
      const q = query();
      return q ? { url: this.url, params: toParams(q) } : undefined;
    });
  }

  departments() {
    return httpResource<Department[]>(() => `${environment.apiBaseUrl}/departments`, {
      defaultValue: [],
    });
  }

  submit(body: SubmitLeaveRequest) {
    return this.http.post<CreatedResponse>(this.url, body);
  }

  cancel(id: string) {
    return this.http.post<void>(`${this.url}/${id}/cancel`, null);
  }

  approve(id: string, note: string | null) {
    return this.http.post<void>(`${this.url}/${id}/approve`, { note });
  }

  reject(id: string, note: string) {
    return this.http.post<void>(`${this.url}/${id}/reject`, { note });
  }
}
