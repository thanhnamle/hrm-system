import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ChangePasswordRequest } from '../../core/models/auth';

/** The signed-in user's own account. */
@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly http = inject(HttpClient);

  /** The API revokes every session of the account afterwards. */
  changePassword(body: ChangePasswordRequest) {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/change-password`, body);
  }
}
