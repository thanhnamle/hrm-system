import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { toParams } from '../../core/http/params';
import { CreatedResponse, PagedResult } from '../../core/models/common';
import {
  CreateDepartmentRequest,
  Department,
  UpdateDepartmentRequest,
} from '../../core/models/department';
import { EmployeeListItem } from '../../core/models/employee';

/** Every call the Departments feature makes to the API. Reads are resources (see EmployeesApi). */
@Injectable({ providedIn: 'root' })
export class DepartmentsApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/departments`;

  list() {
    return httpResource<Department[]>(() => this.url, { defaultValue: [] });
  }

  /** Active members of a department: the only valid choices for its manager. */
  members(departmentId: () => string | undefined) {
    return httpResource<PagedResult<EmployeeListItem>>(() => {
      const id = departmentId();
      return id
        ? {
            url: `${environment.apiBaseUrl}/employees`,
            params: toParams({ departmentId: id, status: 'Active', pageSize: 100 }),
          }
        : undefined;
    });
  }

  create(body: CreateDepartmentRequest) {
    return this.http.post<CreatedResponse>(this.url, body);
  }

  update(id: string, body: UpdateDepartmentRequest) {
    return this.http.put<void>(`${this.url}/${id}`, body);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  assignManager(id: string, employeeId: string) {
    return this.http.put<void>(`${this.url}/${id}/manager`, { employeeId });
  }

  removeManager(id: string) {
    return this.http.delete<void>(`${this.url}/${id}/manager`);
  }
}
