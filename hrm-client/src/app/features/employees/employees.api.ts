import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { toParams } from '../../core/http/params';
import { CreatedResponse, PagedResult } from '../../core/models/common';
import { Department } from '../../core/models/department';
import {
  CreateEmployeeRequest,
  EmployeeDetail,
  EmployeeListItem,
  EmployeeQuery,
  UpdateEmployeeRequest,
} from '../../core/models/employee';

/**
 * Every call the Employees feature makes to the API.
 *
 * Reads are resources: they take a signal-reading function and refetch whenever it changes.
 * They must be created in an injection context (a component field initializer is one).
 * Writes are plain Observables. 400 errors are left to the form; others become a toast.
 */
@Injectable({ providedIn: 'root' })
export class EmployeesApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/employees`;

  list(query: () => EmployeeQuery) {
    return httpResource<PagedResult<EmployeeListItem>>(() => ({
      url: this.url,
      params: toParams(query()),
    }));
  }

  /** Nothing is fetched while the id is undefined (e.g. the form in "create" mode). */
  get(id: () => string | undefined) {
    return httpResource<EmployeeDetail>(() => {
      const value = id();
      return value ? `${this.url}/${value}` : undefined;
    });
  }

  /** For the department filter and select. Features don't import each other, so this is local. */
  departments() {
    return httpResource<Department[]>(() => `${environment.apiBaseUrl}/departments`, {
      defaultValue: [],
    });
  }

  create(body: CreateEmployeeRequest) {
    return this.http.post<CreatedResponse>(this.url, body);
  }

  update(id: string, body: UpdateEmployeeRequest) {
    return this.http.put<void>(`${this.url}/${id}`, body);
  }

  terminate(id: string) {
    return this.http.post<void>(`${this.url}/${id}/terminate`, null);
  }
}
