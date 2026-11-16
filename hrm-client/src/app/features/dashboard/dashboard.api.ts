import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DashboardSummary } from '../../core/models/dashboard';

/** One call for the home page; the API fills only the sections the caller's role gets. */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  summary() {
    return httpResource<DashboardSummary>(() => `${environment.apiBaseUrl}/dashboard`);
  }
}
