import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Role } from '../../core/auth/permissions';
import { CreatedResponse, PagedResult } from '../../core/models/common';
import { EmployeeListItem } from '../../core/models/employee';
import { CreateUserAccountRequest, UserAccount } from '../../core/models/user';

/** Account management (Users.Manage, Admin only). */
@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/users`;

  list() {
    return httpResource<UserAccount[]>(() => this.url, { defaultValue: [] });
  }

  /** Active employees, to pick who gets a new login (the page leaves out those who have one). */
  activeEmployees() {
    return httpResource<PagedResult<EmployeeListItem>>(() => ({
      url: `${environment.apiBaseUrl}/employees`,
      params: { status: 'Active', pageSize: 100 },
    }));
  }

  create(body: CreateUserAccountRequest) {
    return this.http.post<CreatedResponse>(this.url, body);
  }

  changeRole(id: string, role: Role) {
    return this.http.put<void>(`${this.url}/${id}/role`, { role });
  }

  lock(id: string) {
    return this.http.post<void>(`${this.url}/${id}/lock`, null);
  }

  unlock(id: string) {
    return this.http.post<void>(`${this.url}/${id}/unlock`, null);
  }

  resetPassword(id: string, newPassword: string) {
    return this.http.post<void>(`${this.url}/${id}/reset-password`, { newPassword });
  }
}
